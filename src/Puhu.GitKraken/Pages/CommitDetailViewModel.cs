using Akka.Actor;
using Akka.Hosting;
using Puhu.GitKraken.Actors;
using Puhu.GitKraken.Models;
using R3;
using Termina.Reactive;
using Termina.Routing;

namespace Puhu.GitKraken.Pages;

public sealed class CommitDetailViewModel : ReactiveViewModel
{
    [FromRoute(Name = "hash")] private string _hash = "";

    private readonly IActorRef _gitActor;

    public ReactiveProperty<CommitDetail?> Detail { get; } = new(null);
    public ReactiveProperty<string> StatusMessage { get; } = new("Loading...");
    public ReactiveProperty<HashSet<int>> ExpandedFiles { get; } = new([0]);
    public ReactiveProperty<int> SelectedFileIndex { get; } = new(0);

    public CommitDetailViewModel(IActorRegistry actorRegistry)
    {
        _gitActor = actorRegistry.Get<GitRepoActor>();
    }

    public override void OnActivated()
    {
        base.OnActivated();
        _ = LoadCommitAsync();
    }

    private async Task LoadCommitAsync()
    {
        try
        {
            var response = await _gitActor.Ask<object>(new GetCommitDetail(_hash), TimeSpan.FromSeconds(10));

            switch (response)
            {
                case CommitDetailResponse detail:
                    Detail.Value = detail.Detail;
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

    public void ToggleFile(int index)
    {
        var set = new HashSet<int>(ExpandedFiles.Value);
        if (!set.Remove(index))
        {
            set.Add(index);
        }

        ExpandedFiles.Value = set;
    }

    public override void Dispose()
    {
        Detail.Dispose();
        StatusMessage.Dispose();
        ExpandedFiles.Dispose();
        SelectedFileIndex.Dispose();
        base.Dispose();
    }
}
