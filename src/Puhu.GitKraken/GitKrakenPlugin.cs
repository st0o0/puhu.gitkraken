using Akka.Actor;
using Microsoft.Extensions.DependencyInjection;
using Puhu.GitKraken.Actors;
using Puhu.GitKraken.Pages;
using Puhu.GitKraken.Services;
using Puhu.Plugin;

namespace Puhu.GitKraken;

public sealed class GitKrakenPlugin : IPuhuPlugin
{
    public string Name => "GitKraken";

    public void Configure(IPuhuPluginBuilder builder)
    {
        var repoPath = Directory.GetCurrentDirectory();

        builder
            .WithTab("Git", "/git")
            .WithServices(services =>
            {
                services.AddSingleton<GraphRenderer>();
                services.AddSingleton(new GitRepoSettings(repoPath));
                services.AddSingleton<GitCliService>();
            })
            .WithActors((system, registry, resolver) =>
            {
                var cli = resolver.GetService<GitCliService>();
                var actor = system.ActorOf(
                    Props.Create(() => new GitRepoActor(cli)),
                    "gitkraken");
                registry.Register<GitRepoActor>(actor);

                var tickRouter = registry.Get<TickRouterKey>();
                tickRouter.Tell(new RegisterMonitor(
                    "gitkraken", actor, false, TimeSpan.FromSeconds(30)));
            })
            .WithRoutes(termina =>
            {
                termina.RegisterRoute<GraphPage, GraphViewModel>("/git");
                termina.RegisterRoute<CommitDetailPage, CommitDetailViewModel>("/git/commit/{hash}");
            });
    }
}

public sealed record GitRepoSettings(string RepoPath);
