namespace YBot.Command.Abstracts;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class CommandAttribute : Attribute
{
    public string? Prefix { get; init; } = string.Empty;
    public string  Key    { get; init; } = string.Empty;
    public string  Find   { get; init; } = string.Empty;
    public string  Regex  { get; init; } = string.Empty;
    public int     Order  { get; init; } = int.MaxValue;
}