using Akka.Actor;
using Akka.Hosting;
using Akka.Hosting.TestKit;
using Puhu.GitKraken.Actors;
using Puhu.GitKraken.Models;
using Puhu.GitKraken.Services;
using Puhu.GitKraken.Tests.Helpers;

namespace Puhu.GitKraken.Tests.Actors;

public sealed class GitStagingActorTests : TestKit
{
    private readonly TestRepoBuilder _repo = new();

    protected override void ConfigureAkka(AkkaConfigurationBuilder builder, IServiceProvider provider)
    {
    }

    [Fact]
    public async Task GetStatus_returns_unstaged_changes()
    {
        _repo.AddCommit("Initial");
        _repo.WriteFile("file.txt", "modified content");

        var cli = new GitCliService(new GitRepoSettings(_repo.Path));
        var actor = Sys.ActorOf(Props.Create(() => new GitStagingActor(cli)));

        var response = await actor.Ask<StatusResponse>(new GetStatus(), TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.Single(response.Status.Unstaged);
        Assert.Equal(FileStatus.Modified, response.Status.Unstaged[0].Status);
    }

    [Fact]
    public async Task StageFiles_moves_file_to_staged()
    {
        _repo.AddCommit("Initial");
        _repo.WriteFile("file.txt", "modified content");

        var cli = new GitCliService(new GitRepoSettings(_repo.Path));
        var actor = Sys.ActorOf(Props.Create(() => new GitStagingActor(cli)));

        var stageResult = await actor.Ask<OperationResult>(
            new StageFiles(["file.txt"]), TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.True(stageResult.Success);

        var response = await actor.Ask<StatusResponse>(new GetStatus(), TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.Single(response.Status.Staged);
        Assert.Empty(response.Status.Unstaged);
    }

    [Fact]
    public async Task UnstageFiles_moves_file_back_to_unstaged()
    {
        _repo.AddCommit("Initial");
        _repo.WriteFile("file.txt", "modified content");
        _repo.Git("add file.txt");

        var cli = new GitCliService(new GitRepoSettings(_repo.Path));
        var actor = Sys.ActorOf(Props.Create(() => new GitStagingActor(cli)));

        var unstageResult = await actor.Ask<OperationResult>(
            new UnstageFiles(["file.txt"]), TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.True(unstageResult.Success);

        var response = await actor.Ask<StatusResponse>(new GetStatus(), TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.Empty(response.Status.Staged);
        Assert.Single(response.Status.Unstaged);
    }

    [Fact]
    public async Task GetStatus_shows_untracked_files()
    {
        _repo.AddCommit("Initial");
        _repo.WriteFile("newfile.txt", "new content");

        var cli = new GitCliService(new GitRepoSettings(_repo.Path));
        var actor = Sys.ActorOf(Props.Create(() => new GitStagingActor(cli)));

        var response = await actor.Ask<StatusResponse>(new GetStatus(), TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.Single(response.Status.Untracked);
        Assert.Equal("newfile.txt", response.Status.Untracked[0].Path);
    }

    [Fact]
    public async Task StageAll_stages_everything()
    {
        _repo.AddCommit("Initial");
        _repo.WriteFile("file.txt", "modified");
        _repo.WriteFile("newfile.txt", "new");

        var cli = new GitCliService(new GitRepoSettings(_repo.Path));
        var actor = Sys.ActorOf(Props.Create(() => new GitStagingActor(cli)));

        var result = await actor.Ask<OperationResult>(new StageAll(), TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.True(result.Success);

        var response = await actor.Ask<StatusResponse>(new GetStatus(), TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.Equal(2, response.Status.Staged.Count);
        Assert.Empty(response.Status.Unstaged);
        Assert.Empty(response.Status.Untracked);
    }

    protected override async Task AfterAllAsync()
    {
        await base.AfterAllAsync();
        _repo.Dispose();
    }
}
