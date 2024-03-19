using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YBot.Options;

namespace YBot.Services;

public partial class ImageService(
    ILogger<ImageService>        logger,
    IOptionsMonitor<ImageOption> optionsMonitor,
    IHttpClientFactory           httpClientFactory)
{
    [GeneratedRegex("^[\x20-\x7e]*$")]
    private static partial Regex GetTagRegex();

    public async Task<(string? Url, string[]? Tags)> GetAsync(
        IReadOnlyCollection<string> query,
        bool                        appendRandom = false)
    {
        query = query.Where(x => GetTagRegex().IsMatch(x)).ToArray();

        var httpClient = httpClientFactory.CreateClient(nameof(ImageService));
        var url        = optionsMonitor.CurrentValue.GetSearchQuery(ref query, appendRandom);
        logger.LogDebug("Query with tags with {append} append: {tags}", appendRandom, query);

        logger.LogTrace("Fetch: {url}", url);
        var httpResponseMessage = await httpClient.GetAsync(url);
        if (!httpResponseMessage.IsSuccessStatusCode)
        {
            logger.LogWarning("Failed to get image, status code {statusCode}", httpResponseMessage.StatusCode);
            return default;
        }

        var content = await httpResponseMessage.Content.ReadAsStringAsync();

        logger.LogTrace("Image response {response}", content);

        using var document = JsonDocument.Parse(content);

        if (!document.RootElement.TryGetProperty("post", out var post))
        {
            logger.LogInformation("Failed to get image, no post found");
            return default;
        }

        var image  = post.EnumerateArray().FirstOrDefault();
        var sample = image.GetProperty("sample_url").GetString();
        if (string.IsNullOrWhiteSpace(sample)) { sample = image.GetProperty("file_url").GetString(); }

        if (string.IsNullOrWhiteSpace(sample)) { return default; }

        var tags = image.GetProperty("tags").GetString()?.Split(' ');
        return (sample, tags);
    }
}