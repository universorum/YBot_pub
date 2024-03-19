namespace YBot.Command.Abstracts;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class CommandClassAttribute(string? prefix = null) : Attribute
{
    public Scopes Scopes { get; init; } = Scopes.Scoped;
    public string Prefix { get; }       = prefix ?? string.Empty;
}