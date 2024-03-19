using YBot.Command.Abstracts;

namespace YBot.Models;

public class ReceivedMessage(
    string   room,
    string   sender,
    string   message,
    string   userId,
    string[] tag,
    DateTime time,
    string   role,
    string   color) : IMessage
{
    public string   Room    { get; } = room;
    public string   Sender  { get; } = sender;
    public string   Message { get; } = message;
    public string   UserId  { get; } = userId;
    public string[] Tag     { get; } = tag;
    public DateTime Time    { get; } = time;
    public string   Role    { get; } = role;
    public string   Color   { get; } = color;
    public string?  Prefix  { get; set; }

    /// <summary>/cmd a b c => "a b c"</summary>
    public string RawArgument { get; } = message[(message.IndexOf(' ') + 1)..];

    /// <summary>/cmd a b c => "/cmd"</summary>
    public string Command { get; } = message.Split(' ')[0];

    public string PureCommand => Command[(Prefix?.Length ?? 0)..];

    /// <summary>/cmd a b c => [a, b, c]</summary>
    public string[] Arguments { get; } = message.Contains(' ') ? message.Split(' ')[1..] : [];
}