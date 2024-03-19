using System.Net.Http.Headers;

namespace YBot.Options;

public class OpenAiOption
{
    public const string Section = "OpenAI";

    private string _endpoint = null!;

    private string? _formatedSystem;

    private AuthenticationHeaderValue? _headerValue;

    private string _system = null!;

    public required string Endpoint
    {
        get => _endpoint.EndsWith('/') ? _endpoint : $"{_endpoint}/";
        set => _endpoint = value;
    }

    public required string Key         { get; set; }
    public required string Model       { get; set; }
    public required int    MaxToken    { get; set; }
    public required double Temperature { get; set; }
    public required int    Seed        { get; set; }
    public required string Tools       { get; set; }
    public required string Choice      { get; set; }
    public required double Timeout     { get; set; }

    public TimeSpan TimeoutSpan => TimeSpan.FromSeconds(Timeout);

    public required string System
    {
        get => _formatedSystem ??= _system.Replace("\"", @"\""").Replace("\n", @"\n");
        set => _system = value;
    }

    public AuthenticationHeaderValue AuthenticationHeader =>
        _headerValue ??= new AuthenticationHeaderValue("Bearer", Key);

    public string CreateRequest(string userMessage)
    {
        var result = $$"""
                       {
                         "model": "{{Model}}",
                         "messages": [
                           {
                             "role": "system",
                             "content": "{{System}}"
                           },
                           {
                             "role": "user",
                             "content": "{{userMessage}}"
                           }
                         ],
                         "tools": {{Tools}},
                         "tool_choice": {{Choice}},
                         "max_tokens": {{MaxToken}},
                         "temperature": {{Temperature}},
                         "seed": {{Seed}}
                       }
                       """;

        return result;
    }
}