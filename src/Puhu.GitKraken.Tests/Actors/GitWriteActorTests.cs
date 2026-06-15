using Akka.Actor;
using Akka.Hosting;
using Akka.Hosting.TestKit;
using Puhu.GitKraken.Actors;
using Puhu.GitKraken.Models;
using Puhu.GitKraken.Services;
using Puhu.GitKraken.Tests.Helpers;

namespace Puhu.GitKraken.Tests.Actors;

public sealed class GitWriteActorTests : TestKit
{
    private readonly TestRepoBuilder _repo = new();

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
    }

    [Fact]
    public async Task CreateCommit_succeeds_with_staged_files()
    {
        _repo.AddCommit("Initial");
        _repo.WriteFile("file.txt", "modified");
        _repo.Git("add file.txt");

        var cli = new GitCliService(new GitRepoSettings(_repo.Path));
        var actor = Sys.ActorOf(Props.Create(() => new GitWriteActor(cli)));

        var result = await actor.Ask<CommitResult>(
            new CreateCommit("Test commit"), TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.NotNull(result.Sha);
    }

    [Fact]
    public async Task CreateCommit_fails_with_nothing_staged()
    {
        _repo.AddCommit("Initial");

        var cli = new GitCliService(new GitRepoSettings(_repo.Path));
        var actor = Sys.ActorOf(Props.Create(() => new GitWriteActor(cli)));

        var result = await actor.Ask<CommitResult>(
            new CreateCommit("Empty commit"), TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task CreateBranch_and_switch()
    {
        _repo.AddCommit("Initial");

        var cli = new GitCliService(new GitRepoSettings(_repo.Path));
        var actor = Sys.ActorOf(Props.Create(() => new GitWriteActor(cli)));

        var createResult = await actor.Ask<OperationResult>(
            new CreateBranch("feature"), TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.True(createResult.Success);

        var switchResult = await actor.Ask<OperationResult>(
            new SwitchBranch("feature"), TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.True(switchResult.Success);

        var branches = await actor.Ask<BranchesResponse>(
            new GetBranches(), TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        var head = branches.Branches.FirstOrDefault(b => b.IsHead);
        Assert.NotNull(head);
        Assert.Equal("feature", head.Name);
    }

    [Fact]
    public async Task DeleteBranch_works()
    {
        _repo.AddCommit("Initial");
        _repo.Git("branch to-delete");

        var cli = new GitCliService(new GitRepoSettings(_repo.Path));
        var actor = Sys.ActorOf(Props.Create(() => new GitWriteActor(cli)));

        var result = await actor.Ask<OperationResult>(
            new DeleteBranch("to-delete"), TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task MergeBranch_succeeds_fast_forward()
    {
        _repo.AddCommit("Initial");
        // git init creates a default branch — find its name
        var defaultBranch = _repo.Git("rev-parse --abbrev-ref HEAD").Trim();
        _repo.Git("branch feature");
        _repo.Checkout("feature");
        _repo.AddCommit("Feature work", "feature.txt", "content");
        _repo.Checkout(defaultBranch);

        var cli = new GitCliService(new GitRepoSettings(_repo.Path));
        var actor = Sys.ActorOf(Props.Create(() => new GitWriteActor(cli)));

        var result = await actor.Ask<MergeRebaseResult>(
            new MergeBranch("feature"), TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.Equal(ResultKind.Success, result.Kind);
    }

    [Fact]
    public async Task StashChanges_and_pop()
    {
        _repo.AddCommit("Initial");
        _repo.WriteFile("file.txt", "modified content");

        var cli = new GitCliService(new GitRepoSettings(_repo.Path));
        var actor = Sys.ActorOf(Props.Create(() => new GitWriteActor(cli)));

        var stashResult = await actor.Ask<StashResult>(
            new StashChanges("Test stash"), TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.True(stashResult.Success);

        var popResult = await actor.Ask<StashResult>(
            new StashPop(), TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.True(popResult.Success);
    }

    protected override async Task AfterAllAsync()
    {
        await base.AfterAllAsync();
        _repo.Dispose();
    }
}
