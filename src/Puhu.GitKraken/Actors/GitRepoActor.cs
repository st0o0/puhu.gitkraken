using Akka.Actor;
using Puhu.GitKraken.Services;
using Puhu.Plugin;

namespace Puhu.GitKraken.Actors;

public sealed class GitRepoActor : ReceiveActor
{
    private readonly GitCliService _cli;
    private IReadOnlyList<Models.GraphCommit> _cachedGraph = [];
    private string? _currentBranch;
    private string? _lastHeadSha;

    private const string LogFormat = "%H%x00%h%x00%s%x00%aN%x00%aI%x00%P%x00%D";

    public GitRepoActor(GitCliService cli)
    {
        _cli = cli;

        Receive<Tick>(_ => HandleTick());
        Receive<RefreshRequest>(_ => Reload());
        Receive<GetGraph>(HandleGetGraph);
        Receive<GetCommitDetail>(HandleGetCommitDetail);
    }

    private void HandleTick()
    {
        var headResult = _cli.RunAsync("rev-parse HEAD").GetAwaiter().GetResult();
        if (!headResult.Success)
            return;

        var headSha = headResult.Stdout.Trim();
        if (headSha == _lastHeadSha)
            return;

        Reload();
    }

    private void Reload()
    {
        var logResult = _cli.RunAsync($"log --format=\"{LogFormat}\" -200").GetAwaiter().GetResult();
        if (!logResult.Success)
        {
            _cachedGraph = [];
            _currentBranch = null;
            _lastHeadSha = null;
            return;
        }

        _cachedGraph = GitLogParser.Parse(logResult.Stdout);

        var branchResult = _cli.RunAsync("rev-parse --abbrev-ref HEAD").GetAwaiter().GetResult();
        _currentBranch = branchResult.Success ? branchResult.Stdout.Trim() : null;

        var headResult = _cli.RunAsync("rev-parse HEAD").GetAwaiter().GetResult();
        _lastHeadSha = headResult.Success ? headResult.Stdout.Trim() : null;
    }

    private void HandleGetGraph(GetGraph msg)
    {
        if (_cachedGraph.Count == 0)
        {
            var checkResult = _cli.RunAsync("rev-parse --git-dir").GetAwaiter().GetResult();
            if (!checkResult.Success)
            {
                Sender.Tell(new RepoNotFound(_cli.RepoPath));
                return;
            }

            Reload();
        }

        var result = msg.MaxCount < _cachedGraph.Count
            ? _cachedGraph.Take(msg.MaxCount).ToList()
            : _cachedGraph;

        Sender.Tell(new GraphResponse(result, _currentBranch));
    }

    private void HandleGetCommitDetail(GetCommitDetail msg)
    {
        var showResult = _cli.RunAsync($"show --format=\"%H%x00%s%x00%B%x00%aN%x00%aE%x00%aI%x00%P%x00%D\" --no-patch {msg.Hash}")
            .GetAwaiter().GetResult();

        if (!showResult.Success)
        {
            Sender.Tell(new RepoNotFound(_cli.RepoPath));
            return;
        }

        var fields = showResult.Stdout.Trim().Split('\0');
        if (fields.Length < 8)
        {
            Sender.Tell(new RepoNotFound(_cli.RepoPath));
            return;
        }

        var fullSha = fields[0];
        var messageShort = fields[1];
        var messageFull = fields[2].TrimEnd();
        var authorName = fields[3];
        var authorEmail = fields[4];
        var when = DateTimeOffset.Parse(fields[5]);
        var parentShas = string.IsNullOrEmpty(fields[6])
            ? (IReadOnlyList<string>)[]
            : fields[6].Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        var refs = ParseRefs(fields[7]);

        // Try parent diff first, fall back to root diff for initial commits
        var diffResult = _cli.RunAsync($"diff {msg.Hash}~1 {msg.Hash} --unified=3")
            .GetAwaiter().GetResult();

        IReadOnlyList<Models.FileDiff> files = [];
        if (diffResult.Success)
        {
            files = GitDiffParser.Parse(diffResult.Stdout);
        }
        else
        {
            var rootDiffResult = _cli.RunAsync($"diff --root {msg.Hash} --unified=3")
                .GetAwaiter().GetResult();
            if (rootDiffResult.Success)
                files = GitDiffParser.Parse(rootDiffResult.Stdout);
        }

        Sender.Tell(new CommitDetailResponse(new Models.CommitDetail(
            Sha: fullSha[..7],
            Message: messageFull,
            AuthorName: authorName,
            AuthorEmail: authorEmail,
            When: when,
            ParentShas: parentShas,
            BranchLabels: refs.Branches,
            TagLabels: refs.Tags,
            Files: files)));
    }

    private static (List<string> Branches, List<string> Tags) ParseRefs(string refString)
    {
        var branches = new List<string>();
        var tags = new List<string>();

        if (string.IsNullOrWhiteSpace(refString))
            return (branches, tags);

        foreach (var r in refString.Split(',', StringSplitOptions.TrimEntries))
        {
            if (r.StartsWith("tag: "))
                tags.Add(r[5..]);
            else if (r.StartsWith("HEAD -> "))
                branches.Add(r[8..]);
            else if (r is not "HEAD")
                branches.Add(r);
        }

        return (branches, tags);
    }
}
