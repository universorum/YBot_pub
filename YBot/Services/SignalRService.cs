using Microsoft.Extensions.Hosting;

namespace YBot.Services;

public class SignalRService(SignalRClient client) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken) { await client.Start(); }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        client.Stop();
        return Task.CompletedTask;
    }
}