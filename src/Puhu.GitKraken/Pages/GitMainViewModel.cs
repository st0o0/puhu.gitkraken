using Akka.Actor;
using Akka.Hosting;
using Puhu.GitKraken.Actors;
using Puhu.GitKraken.Models;
using Puhu.GitKraken.Services;
using R3;
using Termina.Reactive;

namespace Puhu.GitKraken.Pages;

public sealed class GitMainViewModel : ReactiveViewModel
{
    private readonly IActorRef _repoActor;
    private readonly IActorRef _stagingActor;
    private readonly IActorRef _writeActor;
    private readonly IActorRef _remoteActor;
    private readonly GraphRenderer _renderer;

    public ReactiveProperty<IReadOnlyList<RenderedGraphRow>> GraphRows { get; } = new([]);
    public ReactiveProperty<string> CurrentBranch { get; } = new("");
    public ReactiveProperty<int> CommitCount { get; } = new(0);
    public ReactiveProperty<WorkingTreeStatus> Status { get; } = new(new WorkingTreeStatus([], [], []));
    public ReactiveProperty<string> CommitMessage { get; } = new("");
    public ReactiveProperty<string> StatusMessage { get; } = new("Loading...");

    public GitMainViewModel(IActorRegistry actorRegistry, GraphRenderer renderer)
    {
        _repoActor = actorRegistry.Get<GitRepoActor>();
        _stagingActor = actorRegistry.Get<GitStagingActor>();
        _writeActor = actorRegistry.Get<GitWriteActor>();
        _remoteActor = actorRegistry.Get<GitRemoteActor>();
        _renderer = renderer;
    }

    public IActorRef WriteActor => _writeActor;
    public IActorRef RemoteActor => _remoteActor;
    public IActorRef StagingActor => _stagingActor;

    public override void OnActivated()
    {
        base.OnActivated();
        _ = LoadAllAsync();
    }

    public async Task LoadAllAsync()
    {
        await Task.WhenAll(LoadGraphAsync(), LoadStatusAsync());
    }

    public async Task LoadGraphAsync()
    {
        try
        {
            var response = await _repoActor.Ask<object>(new GetGraph(), TimeSpan.FromSeconds(10));
            switch (response)
            {
                case GraphResponse graph:
                    GraphRows.Value = _renderer.Render(graph.Commits);
                    CurrentBranch.Value = graph.CurrentBranch ?? "detached";
                    CommitCount.Value = graph.Commits.Count;
                    StatusMessage.Value = "";
                    break;
                case RepoNotFound notFound:
                    StatusMessage.Value = $"Repository not found: {notFound.Path}";
                    break;
            }
        }
        catch (Exception ex)
        {
            StatusMessage.Value = $"Error: {ex.Message}";
        }
        RequestRedraw();
    }

    public async Task LoadStatusAsync()
    {
        try
        {
            var response = await _stagingActor.Ask<StatusResponse>(new GetStatus(), TimeSpan.FromSeconds(10));
            Status.Value = response.Status;
        }
        catch (Exception ex)
        {
            StatusMessage.Value = $"Status error: {ex.Message}";
        }
        RequestRedraw();
    }

    public void Refresh()
    {
        _repoActor.Tell(new RefreshRequest());
        StatusMessage.Value = "Refreshing...";
        _ = LoadAllAsync();
    }

    public async Task<OperationResult> StageFileAsync(string path)
    {
        var result = await _stagingActor.Ask<OperationResult>(new StageFiles([path]), TimeSpan.FromSeconds(5));
        if (result.Success) await LoadStatusAsync();
        return result;
    }

    public async Task<OperationResult> UnstageFileAsync(string path)
    {
        var result = await _stagingActor.Ask<OperationResult>(new UnstageFiles([path]), TimeSpan.FromSeconds(5));
        if (result.Success) await LoadStatusAsync();
        return result;
    }

    public async Task<OperationResult> StageAllAsync()
    {
        var result = await _stagingActor.Ask<OperationResult>(new StageAll(), TimeSpan.FromSeconds(5));
        if (result.Success) await LoadStatusAsync();
        return result;
    }

    public async Task<CommitResult> CommitAsync()
    {
        var message = CommitMessage.Value.Trim();
        if (string.IsNullOrEmpty(message))
            return new CommitResult(false, null, "Commit message required");
        if (Status.Value.Staged.Count == 0)
            return new CommitResult(false, null, "Nothing to commit — stage files first");

        var result = await _writeActor.Ask<CommitResult>(new CreateCommit(message), TimeSpan.FromSeconds(10));
        if (result.Success)
        {
            CommitMessage.Value = "";
            _repoActor.Tell(new RefreshRequest());
            await LoadAllAsync();
        }
        return result;
    }

    public async Task<FileDiff> GetFileDiffAsync(string path, bool staged)
    {
        var response = await _stagingActor.Ask<FileDiffResponse>(new GetFileDiff(path, staged), TimeSpan.FromSeconds(5));
        return response.Diff;
    }

    public override void Dispose()
    {
        GraphRows.Dispose();
        CurrentBranch.Dispose();
        CommitCount.Dispose();
        Status.Dispose();
        CommitMessage.Dispose();
        StatusMessage.Dispose();
        base.Dispose();
    }
}
