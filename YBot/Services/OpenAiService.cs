using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Registry;
using Polly.Timeout;
using YBot.Models;
using YBot.Options;

namespace YBot.Services;

public class OpenAiService(
    ILogger<OpenAiService>             logger,
    IOptionsMonitor<OpenAiOption>      options,
    IHttpClientFactory                 httpClientFactory,
    ResiliencePipelineProvider<string> resiliencePipelineProvider)
{
    private static readonly MediaTypeHeaderValue MediaTypeName =
        MediaTypeHeaderValue.Parse(MediaTypeNames.Application.Json);

    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly ResiliencePipeline _pipeline = resiliencePipelineProvider.GetPipeline(nameof(OpenAiService));

    public async Task<ParsedTag?> GetTagsAsync(string prompt, CancellationToken ct = default)
    {
        var request = options.CurrentValue.CreateRequest(prompt);
        logger.LogTrace("OpenAI request: \n{request}", request);

        using var httpContent = new StringContent(request, MediaTypeName);
        using var httpClient  = httpClientFactory.CreateClient(nameof(OpenAiService));

        HttpResponseMessage httpResponseMessage;
        try
        {
            httpResponseMessage = await _pipeline.ExecuteAsync(static async (state, cancellationToken) =>
                        await state.Client.PostAsync("chat/completions", state.Content, cancellationToken),
                    (Client: httpClient, Content: httpContent),
                    ct)
                .ConfigureAwait(false);
        }
        catch (TimeoutRejectedException)
        {
            logger.LogInformation("OpenAI response timed out");
            throw;
        }

        if (!httpResponseMessage.IsSuccessStatusCode)
        {
            logger.LogWarning("Failed to get image, status code {statusCode}", httpResponseMessage.StatusCode);
            return null;
        }

        var raw = await httpResponseMessage.Content.ReadAsStringAsync(ct);
        logger.LogTrace("OpenAI response: \n{response}", raw);

        httpResponseMessage.Dispose();

        var response = JsonSerializer.Deserialize<JsonDocument>(raw, _jsonOptions);
        if (response == default) { return null; }

        var root    = response.RootElement;
        var level   = root.GetProperty("choices");
        var choices = level.EnumerateArray();
        if (!choices.MoveNext())
        {
            logger.LogWarning("OpenAI response invalid: {response}", raw);
            return null;
        }

        var message = choices.Current.GetProperty("message");
        var calls   = message.GetProperty("tool_calls").EnumerateArray();
        if (!calls.MoveNext())
        {
            logger.LogWarning("OpenAI response invalid: {response}", raw);
            return null;
        }

        var function  = calls.Current.GetProperty("function");
        var arguments = function.GetProperty("arguments").GetString();

        if (arguments == null)
        {
            logger.LogWarning("OpenAI response invalid: {response}", raw);
            return null;
        }

        var content = JsonSerializer.Deserialize<ParsedTag>(arguments, _jsonOptions);
        logger.LogDebug("OpenAI response: {response}", arguments);
        return content;
    }
}