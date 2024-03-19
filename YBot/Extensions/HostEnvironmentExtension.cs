using System.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace YBot.Extensions;

public static class HostEnvironmentExtension
{
    [Conditional("RELEASE")]
    public static void SetProductionEnvironment(this IHostEnvironment self)
    {
        self.EnvironmentName = nameof(Environments.Production);
    }
}