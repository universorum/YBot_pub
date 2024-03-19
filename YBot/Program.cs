using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Polly.Timeout;
using YBot.Extensions;
using YBot.Options;
using YBot.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Environment.SetProductionEnvironment();

builder.Services.AddSystemd();

builder.Services.AddHttpClient(nameof(ImageService));
builder.Services.AddHttpClient(nameof(OpenAiService),
    (provider, client) =>
    {
        var options = provider.GetRequiredService<IOptionsMonitor<OpenAiOption>>();
        client.BaseAddress = new Uri(options.CurrentValue.Endpoint);

        client.DefaultRequestHeaders.Authorization = options.CurrentValue.AuthenticationHeader;
    });

builder.Services.AddResilienceEnricher();
builder.Services.AddResiliencePipeline(nameof(OpenAiService),
    (configure, context) =>
    {
        configure.AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 2,
            Delay            = TimeSpan.Zero,
            ShouldHandle     = new PredicateBuilder().Handle<TimeoutRejectedException>()
        });

        var timeout = context.ServiceProvider.GetRequiredService<IOptionsMonitor<OpenAiOption>>()
            .CurrentValue.TimeoutSpan;
        configure.AddTimeout(timeout);
    });

builder.Services.AddCommand();

builder.Services.AddTransient<DiceParser>();
builder.Services.AddSingleton<ImageService>();
builder.Services.AddSingleton<OpenAiService>();
builder.Services.AddSingleton<SignalRClient>();

builder.Services.AddHostedService<SignalRService>();

builder.Services.Configure<ImageOption>(builder.Configuration.GetSection(ImageOption.Section));
builder.Services.Configure<OpenAiOption>(builder.Configuration.GetSection(OpenAiOption.Section));

var host = builder.Build();

host.Run();