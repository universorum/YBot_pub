using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Polly.Timeout;
using YBot.Command.Abstracts;
using YBot.Models;

namespace YBot.Services.Commands;

[CommandClass("/")]
[CommandClass("!")]
public partial class Image(ILogger<Image> logger, SignalRClient client, ImageService image, OpenAiService openAi)
{
    [GeneratedRegex(@"^https:\/\/(?:\w+)+\.example\.com\/")]
    private static partial Regex UrlRegex();

    [Command(Find = "i", Order = 1)]
    [UsedImplicitly]
    public async Task RandomImage(IMessage message, CancellationToken ct = default)
    {
        var group    = message.Arguments;
        var prompt   = message.PureCommand;
        var showTags = group.Any(static x => x.Equals("show", StringComparison.OrdinalIgnoreCase));

        logger.LogDebug("Roll the image");

        if (prompt.Length > 32)
        {
            logger.LogInformation("Prompt too long");
            return;
        }

        ParsedTag? query;
        try { query = await openAi.GetTagsAsync(prompt, ct); }
        catch (TimeoutRejectedException)
        {
            await client.SendMessageAsync("OpenAI炸了");
            return;
        }

        if (query == null || query.Notallow || query.Unsupported) { return; }

        var (url, tags) = await image.GetAsync([..query.Tags ?? [], "-rating:explicit", "-rating:questionable"],
            !(query.Tags?.Count > 0));

        if (string.IsNullOrWhiteSpace(url)) { return; }

        var result = Rewrite(url);
        if (showTags) { result = $"{result} {string.Join(' ', tags ?? [])}"; }

        logger.LogDebug("Image result {result}", result);

        logger.LogInformation("User {user} roll image, result: {actual}", message.Sender, result);
        await client.SendMessageAsync(result);
    }

    private static string Rewrite(string originalUrl) { return UrlRegex().Replace(originalUrl, "https://example/"); }
}