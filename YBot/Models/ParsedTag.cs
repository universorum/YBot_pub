namespace YBot.Models;

public class ParsedTag
{
    public required IReadOnlyCollection<string>? Tags        { get; set; }
    public required bool                         Notallow    { get; set; }
    public required bool                         Unsupported { get; set; }
}