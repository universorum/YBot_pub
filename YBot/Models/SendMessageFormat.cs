using System.Text.Json.Serialization;

namespace YBot.Models;

public record SendMessageFormat
{
    [JsonPropertyName("senderPublicId")]
    public required string SenderPublicId { get; init; }

    [JsonPropertyName("senderColorToken")]
    public required string SenderColorToken { get; init; }

    [JsonPropertyName("room")]
    public required string Room { get; init; }

    [JsonPropertyName("senderNickName")]
    public required string SenderNickName { get; init; }

    [JsonPropertyName("anchorUsername")]
    public string[] AnchorUsername { get; init; } = [];

    [JsonPropertyName("content")]
    public string Content { get; init; } = string.Empty;

    [JsonPropertyName("date")]
    public DateTime Date { get; init; } = DateTime.UtcNow;

    [JsonPropertyName("eventType")]
    public required MessageEventType EventType { get; init; }

    [JsonPropertyName("payload")]
    public object Payload { get; init; } = string.Empty;
}

public enum MessageEventType
{
    AccessToken,
    JoinRoom,
    SetUserName,
    Msg
}