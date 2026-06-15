using Puhu.Plugin;

namespace Puhu.GitKraken;

public sealed class GitKrakenPlugin : IPuhuPlugin
{
    public string Name => "GitKraken";

    public void Configure(IPuhuPluginBuilder builder)
    {
        builder.WithTab("Git", "/git");
    }
}
