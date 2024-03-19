using Microsoft.CodeAnalysis;

namespace YBot.Command.Generator.Models;

public readonly record struct CommandMeta(
    string        MethodName,
    string?       Prefix,
    string        Predicate,
    PredicateType PredicateType,
    int           Order,
    string        Parent,
    bool          IncludeToken,
    bool          WrapRequired)
{
    private int RegexId
    {
        get
        {
            var result = GetHashCode();
            if (result < 0) { result *= -1; }

            return result;
        }
    }

    public Diagnostic? Diagnostic { get; init; }

    public static CommandMeta Report(Diagnostic diagnostic) { return new CommandMeta { Diagnostic = diagnostic }; }

    public string GenerateRegex()
    {
        if (PredicateType == PredicateType.Key) { return string.Empty; }

        return PredicateType switch
        {
            PredicateType.Key => string.Empty,
            PredicateType.Find =>
                $"private static Regex Regex{RegexId} {{ get; }} = new($\"^{Prefix}[^ ]*{Predicate}\", RegexOptions.IgnoreCase | RegexOptions.Compiled);",
            PredicateType.Regex =>
                $"private static Regex Regex{RegexId} {{ get; }} = new($\"{Predicate}\", RegexOptions.IgnoreCase | RegexOptions.Compiled);",
            // PredicateType.Find =>
            // $"[GeneratedRegex($\"^{Prefix}[^ ]*{Predicate}\", RegexOptions.IgnoreCase)] private static partial Regex Get{RegexId}Regex();",
            // PredicateType.Regex =>
            // $"[GeneratedRegex($\"{Predicate}\", RegexOptions.IgnoreCase)] private static partial Regex Get{RegexId}Regex();",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public string GeneratePredicate()
    {
        return PredicateType switch
        {
            PredicateType.Key => $"x => x.StartsWith(\"{Prefix}{Predicate}\")",
            PredicateType.Find => $"x => Regex{RegexId}.IsMatch(x)",
            PredicateType.Regex when string.IsNullOrWhiteSpace(Prefix) => $"Regex{RegexId}.IsMatch(x)",
            PredicateType.Regex => $"x => test.StartsWith(\"{Prefix}\") && Regex{RegexId}.IsMatch(x)",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public string GenerateAction(int space = 0)
    {
        var spaceString = new string(' ', space);
        var token       = IncludeToken ? ", ct" : string.Empty;
        return WrapRequired
            ? $$"""
                async (provider, message, ct) =>
                {{spaceString}}{
                {{spaceString}}    message.Prefix = "{{Prefix}}";
                {{spaceString}}    await provider.GetRequiredService<global::{{Parent}}>().{{MethodName}}(message{{token}});
                {{spaceString}}    return true;
                {{spaceString}}}
                """
            : $$"""
                (provider, message, ct) =>
                {{spaceString}}{
                {{spaceString}}    message.Prefix = "{{Prefix}}";
                {{spaceString}}    return provider.GetRequiredService<global::{{Parent}}>().{{MethodName}}(message{{token}});
                {{spaceString}}}
                """;
    }
}