using System.Text;
using YBot.Command.Generator.Models;

namespace YBot.Command.Generator.Sources;

public static class ServiceCollectionExtension
{
    public static string Generate(IEnumerable<HandlerMeta> handlers)
    {
        var builder = new StringBuilder(@$"#nullable enable

using Microsoft.Extensions.DependencyInjection;
using YBot.Command.Helpers;

namespace YBot.Extensions;

public static class {nameof(ServiceCollectionExtension)}
{{
    public static void AddCommand(this IServiceCollection self)
    {{");

        foreach (var handler in handlers.Select(x => x.Generate("self")).Distinct())
        {
            builder.Append(@$"
        {handler}");
        }

        builder.Append(@$"

        self.AddSingleton<{nameof(CommandService)}>();
    }}
}}");

        return builder.ToString();
    }
}