using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YBot.Command.Abstracts;

namespace YBot.Services.Commands;

[CommandClass("/")]
public class Dice(ILogger<Dice> logger, IServiceProvider serviceProvider, SignalRClient client)
{
    [Command(Key = "rc")]
    [Command(Key = "cc")]
    [UsedImplicitly]
    public async Task RollC(IMessage message)
    {
        var args = message.Arguments;
        if (args.Length == 0) { return; }

        if (!int.TryParse(args.First(), out var target)) { return; }

        var actual = Random.Shared.Next(1, 101);
        logger.LogDebug("CC Target {target}, role {actual}", target, actual);

        var judge = actual switch
        {
            100                          => "大失敗",
            > 95 when target < 50        => "大失敗",
            1                            => "大成功",
            _ when actual > target       => "失敗",
            _ when actual <= target * .2 => "極限成功",
            _ when actual <= target * .5 => "困難成功",
            _ when actual <= target      => "成功",
            _                            => throw new IndexOutOfRangeException()
        };

        var response = $"{message.Sender}的CC{target}骰出了{actual}，是為《{judge}》";

        logger.LogInformation("User {user} Roll CC: {target}, result: {actual}", message.Sender, target, actual);
        await client.SendMessageAsync(response);
    }

    [Command(Key = nameof(Roll))]
    [Command(Key = "r")]
    [UsedImplicitly]
    public async Task Roll(IMessage message)
    {
        var argument = message.RawArgument;
        if (string.IsNullOrWhiteSpace(argument))
        {
            logger.LogTrace("Empty expression");
            return;
        }

        using var logScope = logger.BeginScope(argument);
        var       parser   = serviceProvider.GetRequiredService<DiceParser>();
        int       result;
        bool?     judge;

        logger.LogTrace("Process dice");
        try
        {
            var tuple = parser.Parse(argument);
            if (tuple == null) { return; }

            (result, judge, _) = tuple.Value;

            logger.LogDebug("result: {result}, {judge}", result, judge);
        }
        catch (ArgumentNullException)
        {
            logger.LogDebug("Expression invalid: {e}", argument);
            return;
        }
        catch (ArgumentException e)
        {
            logger.LogWarning("Dice parser failed: {e}", e);
            return;
        }

        var response = judge == null
            ? $"[{message.Sender}] 的 {argument} 骰出了 {result}"
            : $"[{message.Sender}] 的 {argument} 骰出了 {result}《{(judge.Value ? "成功" : "失敗")}》";

        logger.LogInformation("User {user} Roll dice: {expression}, result: {result}",
            message.Sender,
            argument,
            result);
        await client.SendMessageAsync(response, [message.UserId]);
    }
}