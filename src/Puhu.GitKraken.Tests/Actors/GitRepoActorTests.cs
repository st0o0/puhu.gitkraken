using Akka.Actor;
using Akka.Hosting;
using Akka.Hosting.TestKit;
using Puhu.GitKraken.Actors;
using Puhu.GitKraken.Services;
using Puhu.GitKraken.Tests.Helpers;

namespace Puhu.GitKraken.Tests.Actors;

public sealed class GitRepoActorTests : TestKit
{
    private readonly TestRepoBuilder _repo = new();

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
    }

    [Fact]
    public async Task GetGraph_returns_commits()
    {
        _repo.AddCommit("Initial commit");
        _repo.AddCommit("Second commit");

        var cli = new GitCliService(new GitRepoSettings(_repo.Path));
        var actor = Sys.ActorOf(Props.Create(() => new GitRepoActor(cli)));
        actor.Tell(new RefreshRequest());

        var response = await AwaitAndAsk<GraphResponse>(actor, new GetGraph());

        Assert.Equal(2, response.Commits.Count);
        Assert.Equal("Second commit", response.Commits[0].MessageShort);
        Assert.Equal("Initial commit", response.Commits[1].MessageShort);
    }

    [Fact]
    public async Task GetGraph_with_no_prior_refresh_returns_empty()
    {
        var cli = new GitCliService(new GitRepoSettings(_repo.Path));
        var actor = Sys.ActorOf(Props.Create(() => new GitRepoActor(cli)));

        var response = await actor.Ask<GraphResponse>(
            new GetGraph(), TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.Empty(response.Commits);
    }

    [Fact]
    public async Task GetCommitDetail_returns_diff()
    {
        _repo.AddCommit("Initial", "file.txt", "line1");
        _repo.AddCommit("Change file", "file.txt", "line1\nline2");

        var cli = new GitCliService(new GitRepoSettings(_repo.Path));
        var actor = Sys.ActorOf(Props.Create(() => new GitRepoActor(cli)));
        actor.Tell(new RefreshRequest());

        var graph = await AwaitAndAsk<GraphResponse>(actor, new GetGraph());
        var latestSha = graph.Commits[0].FullSha;

        var detail = await actor.Ask<CommitDetailResponse>(
            new GetCommitDetail(latestSha), TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.Equal("Change file", detail.Detail.Message);
        Assert.NotEmpty(detail.Detail.Files);
    }

    [Fact]
    public async Task RepoNotFound_when_path_invalid()
    {
        var cli = new GitCliService(new GitRepoSettings(@"C:\nonexistent\repo"));
        var actor = Sys.ActorOf(Props.Create(() => new GitRepoActor(cli)));
        actor.Tell(new RefreshRequest());

        var response = await AwaitAndAsk<RepoNotFound>(actor, new GetGraph());

        Assert.IsType<RepoNotFound>(response);
    }

    private async Task<T> AwaitAndAsk<T>(IActorRef actor, object message)
    {
        await Task.Delay(500, TestContext.Current.CancellationToken);
        return await actor.Ask<T>(message, TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }

    protected override async Task AfterAllAsync()
    {
        await base.AfterAllAsync();
        _repo.Dispose();
    }
}
