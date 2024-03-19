using Microsoft.CodeAnalysis;

namespace YBot.Command.Generator;

public static class Diagnostics
{
    public static readonly DiagnosticDescriptor IsNotPublic = new("CM0001",
        "The method is not public",
        "Not-Public method command is not allowed",
        "Design",
        DiagnosticSeverity.Warning,
        true);

    public static readonly DiagnosticDescriptor IsStatic = new("CM0002",
        "The method is static",
        "Static method command is not allowed",
        "Design",
        DiagnosticSeverity.Warning,
        true);

    public static readonly DiagnosticDescriptor IsAbstract = new("CM0003",
        "The method is abstract",
        "The command method muse have body",
        "Design",
        DiagnosticSeverity.Warning,
        true);

    public static readonly DiagnosticDescriptor TooManyPredicate = new("CM0004",
        "Invalid predicate",
        "Too many predicate to be created",
        "Design",
        DiagnosticSeverity.Warning,
        true);

    public static readonly DiagnosticDescriptor InvalidCommandParameter = new("CM0005",
        "The parameter of method is invalid",
        "Only Func<IMessage, Task>, Func<IMessage, Task<bool>>, Func<IMessage, CancellationToken, Task> and Func<IMessage, CancellationToken, Task<bool>> is allowed",
        "Design",
        DiagnosticSeverity.Warning,
        true);
}