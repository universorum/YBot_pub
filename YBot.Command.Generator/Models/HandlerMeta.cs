using YBot.Command.Abstracts;

namespace YBot.Command.Generator.Models;

public readonly record struct HandlerMeta(string Fullname, string Prefix, Scopes Scopes)
{
    public string Generate(string var)
    {
        return Scopes switch
        {
            Scopes.Scoped    => $"{var}.AddScoped<global::{Fullname}>();",
            Scopes.Singleton => $"{var}.AddSingleton<global::{Fullname}>();",
            Scopes.Transient => $"{var}.AddTransient<global::{Fullname}>();",
            _                => throw new ArgumentOutOfRangeException()
        };
    }
}