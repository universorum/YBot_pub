using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using YBot.Command.Helpers;
using YBot.Models;

namespace YBot.Services;

public class SignalRClient : IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        Converters = { new JsonStringEnumConverter(), new JsonDateTimeConverter() }
    };

    private readonly string                 _color;
    private readonly CommandService         _command;
    private readonly HubConnection          _connection;
    private readonly ILogger<SignalRClient> _logger;
    private readonly string                 _name;
    private readonly IHubProxy              _proxy;
    private readonly string                 _room;

    private string _publicId = string.Empty;

    public SignalRClient(ILogger<SignalRClient> logger, IConfiguration configuration, CommandService command)
    {
        _logger     = logger;
        _command    = command;
        _connection = new HubConnection(configuration.GetValue<string>("Uri"));

        _proxy = _connection.CreateHubProxy("chathub");
        _name  = configuration.GetValue<string>("Name")  ?? "BOT";
        _color = configuration.GetValue<string>("Color") ?? "#000000";
        _room  = configuration.GetValue<string>("Room")  ?? throw new NullReferenceException();

        _connection.Closed += Relink;

        var receiveMessage = _proxy.Subscribe("receiveMessage");
        receiveMessage.Received += OnReceived;

        /*
         * Origin rule:
         * send accessSuccess => exec in Start()
         * receive publicID
         * send JoinRoom
         * send SetUserName
         */
        var accessSuccess = _proxy.Subscribe("accessSuccess");
        accessSuccess.Received += OnAccessSuccess;
    }

    public void Dispose()
    {
        _connection.Stop();
        _connection.Dispose();
        GC.SuppressFinalize(this); // CA1816
    }

    private async void Relink()
    {
        try { _connection.Stop(); }
        catch (Exception e) { _logger.LogWarning(e, "Error when stopping connection"); }

        while (_connection.State != ConnectionState.Connected)
        {
            await Task.Delay(5000);
            try { await Start(); }
            catch (Exception e) { _logger.LogError(e, "Error when starting connection"); }
        }
    }

    public async Task Start()
    {
        await _connection.Start();
        await SendMessageAsync(eventType: MessageEventType.AccessToken, payload: new { LoginID = CreateId() });
        _logger.LogInformation("Connected to hub {room}", _room);
    }

    private void OnAccessSuccess(IList<JToken> arguments)
    {
        _publicId = Convert<string>(arguments[0]) ?? throw new NullReferenceException();
        Task.Run(async () =>
        {
            try
            {
                await SendMessageAsync(eventType: MessageEventType.JoinRoom);
                await SendMessageAsync(eventType: MessageEventType.SetUserName);
            }
            catch (Exception e) { _logger.LogError(e, "Error when sending message"); }
        });
    }

    public void Stop() { _connection.Stop(); }


    // on message type "receiveMessage"
    private void OnReceived(IList<JToken> arguments)
    {
        var json = Convert<string>(arguments[0]);
        if (json == null) { return; }

        var room   = Convert<string>(arguments[0])   ?? string.Empty;
        var name   = Convert<string>(arguments[1])   ?? string.Empty;
        var msg    = Convert<string>(arguments[2])   ?? string.Empty;
        var uid    = Convert<string>(arguments[3])   ?? string.Empty;
        var tag    = Convert<string[]>(arguments[4]) ?? [];
        var time   = Convert<string>(arguments[5])   ?? "0";
        var sender = Convert<string>(arguments[6])   ?? string.Empty;
        var color  = Convert<string>(arguments[7])   ?? string.Empty;

        if (uid == _publicId) { return; }

        var dateTime = long.TryParse(time, out var timeLong)
            ? DateTimeOffset.FromUnixTimeMilliseconds(timeLong).DateTime // if time is unix timestamp
            : DateTime.Parse(time);                                      // else time is ymd format

        var message = new ReceivedMessage(room, name, msg, uid, tag, dateTime, sender, color);
        _logger.LogTrace("Received message, Room: {room}, msg: {msg}", room, msg);

        _ = _command.ExecuteAsync(message);
    }

    /// <summary>Send message to Server</summary>
    public async Task SendMessageAsync(
        string?          content        = null,
        string[]?        anchorUsername = null,
        MessageEventType eventType      = MessageEventType.Msg,
        string?          color          = null,
        string?          room           = null,
        string?          name           = null,
        string?          senderPublicId = null,
        object?          payload        = null)
    {
        var message = new SendMessageFormat
        {
            SenderPublicId   = senderPublicId ?? _publicId,
            SenderColorToken = color          ?? _color,
            Room             = room           ?? _room,
            SenderNickName   = name           ?? _name,
            AnchorUsername   = anchorUsername ?? [],
            Content          = content        ?? string.Empty,
            Date             = DateTime.UtcNow,
            EventType        = eventType,
            Payload          = payload ?? string.Empty
        };

        await _proxy.Invoke("SendMessage", JsonSerializer.Serialize(message, SerializerOptions));
    }

    /// <summary> Convert SignalR argument to T, copy form lib. </summary>
    private T? Convert<T>(JToken? obj) { return obj == null ? default : obj.ToObject<T>(_connection.JsonSerializer); }

    private static string CreateId()
    {
        var random  = new Random();
        var builder = new StringBuilder();
        for (var i = 0; i < 40; i++) { builder.Append(random.Next(0, 16).ToString("x")); }

        return builder.ToString();
    }
}