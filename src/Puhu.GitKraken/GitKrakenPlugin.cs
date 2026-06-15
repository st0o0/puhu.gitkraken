using Akka.Actor;
using Akka.DependencyInjection;
using Akka.Hosting;
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
            })
            .WithActors((system, registry, resolver) =>
            {
                var settings = resolver.GetService<GitRepoSettings>();
                var actor = system.ActorOf(
                    Props.Create(() => new GitRepoActor(settings.RepoPath)),
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
