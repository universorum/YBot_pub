using System.Collections.Immutable;
using System.Text;
using YBot.Command.Generator.Models;

namespace YBot.Command.Generator.Sources;

public static class CommandService
{
    public static string Generate(ImmutableArray<CommandMeta> commands)
    {
        var orderList = commands.GroupBy(x => x.Order).ToList();

        var builder = new StringBuilder(@$"#nullable enable

using System.Collections.Frozen;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YBot.Models;
using YBot.Command.Abstracts;

namespace YBot.Command.Helpers;

public partial class {nameof(CommandService)}(ILogger<{nameof(CommandService)}> logger, IServiceScopeFactory scopeFactory)
{{");

        foreach (var regex in commands.Select(command => command.GenerateRegex())
                     .Where(regex => !string.IsNullOrWhiteSpace(regex)))
        {
            builder.Append(@$"
    {regex}
");
        }

        builder.Append(@"
    private readonly List<int> _keyList = [ ");

        foreach (var group in orderList.Select(static x => x.Key).OrderBy(static x => x))
        {
            builder.Append($"{group}, ");
        }

        if (orderList.Count > 0) { builder.Length -= 2; }

        builder.AppendLine(" ];");

        builder.Append(@"
    private readonly FrozenDictionary<int, FrozenSet<CommandHandler>> _predicates =
        new Dictionary<int, FrozenSet<CommandHandler>>
        {");

        foreach (var group in orderList.OrderBy(x => x.Key))
        {
            builder.Append(@$"
            {{
                {group.Key},
                new CommandHandler[]
                {{");

            foreach (var commandMeta in group)
            {
                builder.Append(@$"
                    new CommandHandler
                    {{
                        Key       = ""{commandMeta.Parent}.{commandMeta.MethodName}"",
                        Predicate = static {commandMeta.GeneratePredicate()},
                        Action    = static {commandMeta.GenerateAction(24)},
                    }},");
            }

            builder.Append(@"
                }.ToFrozenSet()
            },");
        }

        builder.Append(@"
        }.ToFrozenDictionary();

    public async Task ExecuteAsync(ReceivedMessage message, CancellationToken ct = default)
    {
        using var logScope = logger.BeginScope(message.Command);
        logger.LogTrace(""Find command by key {key}"", message.Command);

        await using var scope = scopeFactory.CreateAsyncScope();
        foreach (var order in _keyList)
        {
            if(!_predicates.TryGetValue(order, out var predicates))
            { 
                logger.LogWarning(""The key {key} not exists in predicate collection, check source generator"", order);
                continue;
            }

            var sp = scope.ServiceProvider;
            var tasks = predicates.Where(x => x.Predicate.Invoke(message.Command))
                .GroupBy(x => x.Key)
                .Select(x => x.First().Action.Invoke(sp, message, ct))
                .ToArray();
            logger.LogTrace(""Found {size} command in order {order}"", tasks.Length, order);

            try
            {
                var stop = await Task.WhenAll(tasks);
                if (stop.Any(static x => x))
                {
                    logger.LogTrace(""Skip other handler than order > {order}"", order);
                    break;
                }
            }
            catch (Exception ex)  { logger.LogWarning(ex, ""Error in command handler""); }

            logger.LogTrace(""Go next order"");
        }
    }

    private class CommandHandler
    {
        public required string                                                          Key       { get; init; }
        public required Func<string, bool>                                              Predicate { get; init; }
        public required Func<IServiceProvider, IMessage, CancellationToken, Task<bool>> Action    { get; init; }
    }
} ");

        return builder.ToString();
    }
}