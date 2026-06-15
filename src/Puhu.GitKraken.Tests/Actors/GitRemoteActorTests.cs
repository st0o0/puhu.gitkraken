using Akka.Actor;
using Akka.Hosting;
using Akka.Hosting.TestKit;
using Puhu.GitKraken.Actors;
using Puhu.GitKraken.Services;
using Puhu.GitKraken.Tests.Helpers;

namespace Puhu.GitKraken.Tests.Actors;

public sealed class GitRemoteActorTests : TestKit
{
    private readonly TestRepoBuilder _repo = new();
    private readonly TestRepoBuilder _remoteRepo = new();

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
    }

    [Fact]
    public async Task GetRemotes_returns_configured_remotes()
    {
        _repo.AddCommit("Initial");
        _repo.Git($"remote add origin \"{_remoteRepo.Path}\"");

        var cli = new GitCliService(new GitRepoSettings(_repo.Path));
        var actor = Sys.ActorOf(Props.Create(() => new GitRemoteActor(cli)));

        var response = await actor.Ask<RemotesResponse>(
            new GetRemotes(), TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.Single(response.Remotes);
        Assert.Equal("origin", response.Remotes[0].Name);
    }

    [Fact]
    public async Task GetRemotes_returns_empty_when_no_remotes()
    {
        _repo.AddCommit("Initial");

        var cli = new GitCliService(new GitRepoSettings(_repo.Path));
        var actor = Sys.ActorOf(Props.Create(() => new GitRemoteActor(cli)));

        var response = await actor.Ask<RemotesResponse>(
            new GetRemotes(), TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.Empty(response.Remotes);
    }

    [Fact]
    public async Task Fetch_succeeds_with_local_remote()
    {
        _remoteRepo.AddCommit("Remote commit");
        _repo.AddCommit("Local commit");
        _repo.Git($"remote add origin \"{_remoteRepo.Path}\"");

        var cli = new GitCliService(new GitRepoSettings(_repo.Path));
        var actor = Sys.ActorOf(Props.Create(() => new GitRemoteActor(cli)));

        var result = await actor.Ask<RemoteOperationResult>(
            new GitFetch("origin"), TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        Assert.True(result.Success);
    }

    protected override async Task AfterAllAsync()
    {
        await base.AfterAllAsync();
        _repo.Dispose();
        _remoteRepo.Dispose();
    }
}
