using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using YBot.Command.Abstracts;
using YBot.Command.Generator.Models;
using YBot.Command.Generator.Sources;

namespace YBot.Command.Generator;

[Generator]
public class CommandGenerator : IIncrementalGenerator
{
    private static readonly string CommandAttributeName = typeof(CommandAttribute).FullName!;
    private static readonly string HandlerAttributeName = typeof(CommandClassAttribute).FullName!;
    private static readonly string MessageName          = typeof(IMessage).FullName!;
    private static readonly string TaskName             = typeof(Task).FullName!;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var handlers = context.SyntaxProvider.ForAttributeWithMetadataName(HandlerAttributeName,
                static (node, _) => node is ClassDeclarationSyntax,
                TransformHandler)
            .Collect();

        var commands = context.SyntaxProvider.ForAttributeWithMetadataName(CommandAttributeName,
                static (node, _) => node is MethodDeclarationSyntax,
                TransformCommand)
            .Collect();

        var provider = commands.Combine(handlers);

        context.RegisterSourceOutput(provider, Execute);
    }

    private static void Execute(
        SourceProductionContext context,
        (ImmutableArray<EquatableArray<CommandMeta>> Command, ImmutableArray<EquatableArray<HandlerMeta>> Handler)
            values)
    {
        var (commandArrays, handlerArrays) = values;

        var commands           = commandArrays.SelectMany(static x => x).ToImmutableArray();
        var commandDiagnostics = commands.Where(x => x.Diagnostic != null).ToImmutableArray();
        commands = commands.Where(x => !commandDiagnostics.Contains(x)).ToImmutableArray();
        var commandGroup = commands.GroupBy(static x => x.Parent);

        foreach (var commandMeta in commandDiagnostics) { context.ReportDiagnostic(commandMeta.Diagnostic!); }

        var handlerDict = handlerArrays.SelectMany(static x => x)
            .GroupBy(static x => x.Fullname)
            .ToImmutableDictionary(static x => x.Key, static x => x.ToImmutableHashSet());

        var commandSet = new HashSet<CommandMeta>();
        var handlerSet = new HashSet<HandlerMeta>();
        foreach (var commandList in commandGroup)
        {
            if (!handlerDict.TryGetValue(commandList.Key, out var handlerList))
            {
                // TODO Diagnostic
                continue;
            }

            foreach (var handler in handlerList) { handlerSet.Add(handler); }

            var overrideGroup = commandList.GroupBy(static x => !string.IsNullOrWhiteSpace(x.Prefix))
                .ToDictionary(x => x.Key, x => x.ToArray());

            if (overrideGroup.TryGetValue(true, out var overrided))
            {
                foreach (var meta in overrided) { commandSet.Add(meta); }
            }

            foreach (var handler in handlerList)
            {
                if (!overrideGroup.TryGetValue(false, out overrided)) { continue; }

                foreach (var meta in overrided) { commandSet.Add(meta with { Prefix = handler.Prefix }); }
            }
        }

        var content = CommandService.Generate(commandSet.ToImmutableArray());
        context.AddSource($"{nameof(CommandService)}.g.cs", SourceText.From(content, Encoding.UTF8));

        content = ServiceCollectionExtension.Generate(handlerSet);
        context.AddSource($"{nameof(ServiceCollectionExtension)}.g.cs", SourceText.From(content, Encoding.UTF8));
    }

    private static EquatableArray<CommandMeta> TransformCommand(
        GeneratorAttributeSyntaxContext context,
        CancellationToken               ct)
    {
        var list        = new HashSet<CommandMeta>();
        var declaration = (MethodDeclarationSyntax)context.TargetNode;

        if (!Is(declaration.Modifiers, SyntaxKind.PublicKeyword))
        {
            list.Add(CommandMeta.Report(Diagnostic.Create(Diagnostics.IsNotPublic, declaration.GetLocation())));
        }

        if (Is(declaration.Modifiers, SyntaxKind.StaticKeyword))
        {
            list.Add(CommandMeta.Report(Diagnostic.Create(Diagnostics.IsStatic, declaration.GetLocation())));
        }

        if (Is(declaration.Modifiers, SyntaxKind.AbstractKeyword))
        {
            list.Add(CommandMeta.Report(Diagnostic.Create(Diagnostics.IsAbstract, declaration.GetLocation())));
        }

        var symbol     = (IMethodSymbol)context.TargetSymbol;
        var parameters = symbol.Parameters;
        var failed = parameters.Length switch
        {
            1 => parameters[0].Type.ToDisplayString() != MessageName,
            2 => parameters[0].Type.ToDisplayString() != MessageName ||
                 parameters[1].Type.ToDisplayString() != typeof(CancellationToken).FullName,
            _ => true
        };

        var returnedType = symbol.ReturnType;
        var wrapRequired = returnedType.ToDisplayString() == TaskName;

        if (failed)
        {
            list.Add(CommandMeta.Report(Diagnostic.Create(Diagnostics.InvalidCommandParameter,
                declaration.GetLocation())));
        }

        if (list.Count > 0) { return new EquatableArray<CommandMeta>(list.ToImmutableArray()); }

        var includeToken = parameters.Length == 2;
        var parent       = symbol.ContainingType.ToDisplayString();
        var name         = symbol.Name;
        foreach (var attributeData in GetTargetAttribute(context.Attributes, CommandAttributeName))
        {
            TryGetValue<string?>(attributeData.NamedArguments, nameof(CommandAttribute.Prefix), out var prefix);
            TryGetValue<string?>(attributeData.NamedArguments, nameof(CommandAttribute.Key), out var key);
            TryGetValue<string?>(attributeData.NamedArguments, nameof(CommandAttribute.Find), out var find);
            TryGetValue<string?>(attributeData.NamedArguments, nameof(CommandAttribute.Regex), out var regex);

            if (!TryGetValue<int>(attributeData.NamedArguments, nameof(CommandAttribute.Order), out var order))
            {
                order = int.MaxValue;
            }

            var           valid     = 0;
            var           predicate = string.Empty;
            PredicateType type      = default;
            if (!string.IsNullOrWhiteSpace(key))
            {
                valid++;
                predicate = key;
                type      = PredicateType.Key;
            }

            if (!string.IsNullOrWhiteSpace(find))
            {
                valid++;
                predicate = find;
                type      = PredicateType.Find;
            }

            if (!string.IsNullOrWhiteSpace(regex))
            {
                valid++;
                predicate = regex;
                type      = PredicateType.Regex;
            }

            if (valid == 1)
            {
                list.Add(new CommandMeta(name, prefix, predicate!, type, order, parent, includeToken, wrapRequired));
            }
            else
            {
                list.Add(CommandMeta.Report(Diagnostic.Create(Diagnostics.TooManyPredicate,
                    declaration.GetLocation())));
            }
        }

        return list.Count == 0
            ? new EquatableArray<CommandMeta>(ImmutableArray<CommandMeta>.Empty)
            : new EquatableArray<CommandMeta>(list.ToImmutableArray());
    }

    private static EquatableArray<HandlerMeta> TransformHandler(
        GeneratorAttributeSyntaxContext context,
        CancellationToken               ct)
    {
        var list     = new HashSet<HandlerMeta>();
        var fullname = context.TargetSymbol.ToDisplayString();
        foreach (var attributeData in GetTargetAttribute(context.Attributes, HandlerAttributeName))
        {
            TryGetValue<string?>(attributeData.ConstructorArguments, 0, out var prefix);
            prefix ??= string.Empty;

            TryGetValue<Scopes?>(attributeData.ConstructorArguments, 0, out var scopes);
            scopes ??= Scopes.Scoped;

            list.Add(new HandlerMeta(fullname, prefix, scopes.Value));
        }

        return list.Count == 0
            ? new EquatableArray<HandlerMeta>(ImmutableArray<HandlerMeta>.Empty)
            : new EquatableArray<HandlerMeta>(list.ToImmutableArray());
    }

    private static bool Is(SyntaxTokenList tokenList, SyntaxKind kind)
    {
        for (var i = 0; i < tokenList.Count; i++)
        {
            if (tokenList[i].IsKind(kind)) { return true; }
        }

        return false;
    }

    private static IEnumerable<AttributeData> GetTargetAttribute(
        ImmutableArray<AttributeData> list,
        string                        metadataName)
    {
        for (var i = 0; i < list.Length; i++)
        {
            var current = list[i];
            if (current.AttributeClass?.ToDisplayString() == metadataName) { yield return current; }
        }
    }

    private static bool TryGetValue<T>(ImmutableArray<TypedConstant> list, int index, out T? result)
    {
        result = default;
        if (list.Length <= index) { return false; }

        var value = list[index].Value;
        switch (value)
        {
            case null: return true;
            case T r:
                result = r;
                return true;
            default: return false;
        }
    }

    private static bool TryGetValue<T>(
        ImmutableArray<KeyValuePair<string, TypedConstant>> list,
        string                                              name,
        out T?                                              result)
    {
        result = default;

        for (var i = 0; i < list.Length; i++)
        {
            var current = list[i];
            if (current.Key != name) { continue; }

            var value = current.Value.Value;
            switch (value)
            {
                case null: return true;
                case T r:
                    result = r;
                    return true;
                default: return false;
            }
        }

        return false;
    }
}