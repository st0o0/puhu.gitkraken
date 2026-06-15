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
                services.AddSingleton<CommandRegistry>();
            })
            .WithActors((system, registry, resolver) =>
            {
                var cli = resolver.GetService<GitCliService>();

                var repoActor = system.ActorOf(
                    Props.Create(() => new GitRepoActor(cli)),
                    "gitkraken-repo");
                registry.Register<GitRepoActor>(repoActor);

                var stagingActor = system.ActorOf(
                    Props.Create(() => new GitStagingActor(cli)),
                    "gitkraken-staging");
                registry.Register<GitStagingActor>(stagingActor);

                var writeActor = system.ActorOf(
                    Props.Create(() => new GitWriteActor(cli)),
                    "gitkraken-write");
                registry.Register<GitWriteActor>(writeActor);

                var remoteActor = system.ActorOf(
                    Props.Create(() => new GitRemoteActor(cli)),
                    "gitkraken-remote");
                registry.Register<GitRemoteActor>(remoteActor);

                var tickRouter = registry.Get<TickRouterKey>();
                tickRouter.Tell(new RegisterMonitor(
                    "gitkraken-repo", repoActor, false, TimeSpan.FromSeconds(30)));
            })
            .WithRoutes(termina =>
            {
                termina.RegisterRoute<GitMainPage, GitMainViewModel>("/git");
                termina.RegisterRoute<CommitDetailPage, CommitDetailViewModel>("/git/commit/{hash}");
                termina.RegisterRoute<CommandPalettePage, CommandPaletteViewModel>("/git/palette");
            });
    }
}

public sealed record GitRepoSettings(string RepoPath);
