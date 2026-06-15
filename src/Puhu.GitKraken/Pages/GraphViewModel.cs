using Akka.Actor;
using Akka.Hosting;
using Puhu.GitKraken.Actors;
using Puhu.GitKraken.Services;
using R3;
using Termina.Reactive;

namespace Puhu.GitKraken.Pages;

public sealed class GraphViewModel : ReactiveViewModel
{
    private readonly IActorRef _gitActor;
    private readonly GraphRenderer _renderer;

    public ReactiveProperty<IReadOnlyList<RenderedGraphRow>> Rows { get; } = new([]);
    public ReactiveProperty<string> CurrentBranch { get; } = new("");
    public ReactiveProperty<int> CommitCount { get; } = new(0);
    public ReactiveProperty<string> StatusMessage { get; } = new("Loading...");

    public GraphViewModel(IActorRegistry actorRegistry, GraphRenderer renderer)
    {
        _gitActor = actorRegistry.Get<GitRepoActor>();
        _renderer = renderer;
    }

    public override void OnActivated()
    {
        base.OnActivated();
        _ = LoadGraphAsync();
    }

    public async Task LoadGraphAsync()
    {
        try
        {
            var response = await _gitActor.Ask<object>(new GetGraph(), TimeSpan.FromSeconds(10));

            switch (response)
            {
                case GraphResponse graph:
                    var rows = _renderer.Render(graph.Commits);
                    Rows.Value = rows;
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

    public void Refresh()
    {
        _gitActor.Tell(new RefreshRequest());
        StatusMessage.Value = "Refreshing...";
        _ = LoadGraphAsync();
    }

    public override void Dispose()
    {
        Rows.Dispose();
        CurrentBranch.Dispose();
        CommitCount.Dispose();
        StatusMessage.Dispose();
        base.Dispose();
    }
}
