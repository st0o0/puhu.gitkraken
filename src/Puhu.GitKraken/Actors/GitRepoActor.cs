using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Akka.Actor;
using LibGit2Sharp;
using Puhu.GitKraken.Models;
using Puhu.Plugin;
using ChangeKind = Puhu.GitKraken.Models.ChangeKind;
using LibChangeKind = LibGit2Sharp.ChangeKind;

namespace Puhu.GitKraken.Actors;

public sealed partial class GitRepoActor : ReceiveActor
{
    private readonly string _repoPath;
    private List<GraphCommit> _cachedGraph = [];
    private string? _currentBranch;
    private string? _lastHeadSha;
    private bool _repoValid;

    public GitRepoActor(string repoPath)
    {
        _repoPath = repoPath;

        Receive<Tick>(_ => HandleTick());
        Receive<RefreshRequest>(_ => Reload());
        Receive<GetGraph>(HandleGetGraph);
        Receive<GetCommitDetail>(HandleGetCommitDetail);
    }

    private void HandleTick()
    {
        if (!_repoValid && !Repository.IsValid(_repoPath))
        {
            return;
        }

        try
        {
            using var repo = new Repository(_repoPath);
            var headSha = repo.Head?.Tip?.Sha;
            if (headSha == _lastHeadSha)
            {
                return;
            }
        }
        catch
        {
            return;
        }

        Reload();
    }

    private void Reload()
    {
        if (!TryOpenRepo(out var repo))
        {
            _repoValid = false;
            _cachedGraph = [];
            _currentBranch = null;
            _lastHeadSha = null;
            return;
        }

        using (repo)
        {
            _repoValid = true;
            _currentBranch = repo.Head?.FriendlyName;
            _lastHeadSha = repo.Head?.Tip?.Sha;

            var branchTips = new Dictionary<string, List<string>>();
            foreach (var branch in repo.Branches)
            {
                if (branch.Tip is null)
                {
                    continue;
                }

                if (!branchTips.TryGetValue(branch.Tip.Sha, out var labels))
                {
                    labels = [];
                    branchTips[branch.Tip.Sha] = labels;
                }
                labels.Add(branch.FriendlyName);
            }

            var tagTips = new Dictionary<string, List<string>>();
            foreach (var tag in repo.Tags)
            {
                var target = tag.PeeledTarget as Commit ?? tag.Target as Commit;
                if (target is null)
                {
                    continue;
                }

                if (!tagTips.TryGetValue(target.Sha, out var labels))
                {
                    labels = [];
                    tagTips[target.Sha] = labels;
                }
                labels.Add(tag.FriendlyName);
            }

            var commits = new List<GraphCommit>();
            foreach (var c in repo.Commits.QueryBy(new CommitFilter
            {
                SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Time,
            }).Take(200))
            {
                commits.Add(new GraphCommit(
                    Sha: c.Sha[..7],
                    FullSha: c.Sha,
                    MessageShort: c.MessageShort,
                    AuthorName: c.Author.Name,
                    When: c.Author.When,
                    ParentShas: c.Parents.Select(p => p.Sha).ToList(),
                    BranchLabels: branchTips.GetValueOrDefault(c.Sha) ?? [],
                    TagLabels: tagTips.GetValueOrDefault(c.Sha) ?? []));
            }

            _cachedGraph = commits;
        }
    }

    private void HandleGetGraph(GetGraph msg)
    {
        if (!_repoValid && _cachedGraph.Count == 0)
        {
            if (!Repository.IsValid(_repoPath))
            {
                Sender.Tell(new RepoNotFound(_repoPath));
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
        if (!TryOpenRepo(out var repo))
        {
            Sender.Tell(new RepoNotFound(_repoPath));
            return;
        }

        using (repo)
        {
            var commit = repo.Lookup<Commit>(msg.Hash);
            if (commit is null)
            {
                Sender.Tell(new RepoNotFound(_repoPath));
                return;
            }

            var branchLabels = repo.Branches
                .Where(b => b.Tip?.Sha == commit.Sha)
                .Select(b => b.FriendlyName)
                .ToList();

            var tagLabels = repo.Tags
                .Where(t => (t.PeeledTarget as Commit ?? t.Target as Commit)?.Sha == commit.Sha)
                .Select(t => t.FriendlyName)
                .ToList();

            var files = new List<FileDiff>();
            var parent = commit.Parents.FirstOrDefault();
            var diff = parent is not null
                ? repo.Diff.Compare<Patch>(parent.Tree, commit.Tree)
                : repo.Diff.Compare<Patch>(repo.ObjectDatabase.CreateTree(new TreeDefinition()), commit.Tree);

            foreach (var change in diff)
            {
                var kind = change.Status switch
                {
                    LibChangeKind.Added => ChangeKind.Added,
                    LibChangeKind.Deleted => ChangeKind.Deleted,
                    LibChangeKind.Renamed => ChangeKind.Renamed,
                    _ => ChangeKind.Modified,
                };

                var hunks = new List<DiffHunk>();
                var currentLines = new List<DiffLine>();
                var oldStart = 0;
                var oldCount = 0;
                var newStart = 0;
                var newCount = 0;

                foreach (var line in change.Patch.Split('\n'))
                {
                    if (line.StartsWith("@@"))
                    {
                        if (currentLines.Count > 0)
                        {
                            hunks.Add(new DiffHunk(oldStart, oldCount, newStart, newCount, currentLines));
                            currentLines = [];
                        }
                        ParseHunkHeader(line, out oldStart, out oldCount, out newStart, out newCount);
                    }
                    else if (line.StartsWith('+') && !line.StartsWith("+++"))
                    {
                        currentLines.Add(new DiffLine(DiffLineKind.Added, line[1..]));
                    }
                    else if (line.StartsWith('-') && !line.StartsWith("---"))
                    {
                        currentLines.Add(new DiffLine(DiffLineKind.Removed, line[1..]));
                    }
                    else if (line.StartsWith(' '))
                    {
                        currentLines.Add(new DiffLine(DiffLineKind.Context, line[1..]));
                    }
                }

                if (currentLines.Count > 0)
                {
                    hunks.Add(new DiffHunk(oldStart, oldCount, newStart, newCount, currentLines));
                }

                files.Add(new FileDiff(
                    change.Path,
                    kind,
                    change.OldPath != change.Path ? change.OldPath : null,
                    hunks));
            }

            Sender.Tell(new CommitDetailResponse(new CommitDetail(
                Sha: commit.Sha[..7],
                Message: commit.Message.TrimEnd(),
                AuthorName: commit.Author.Name,
                AuthorEmail: commit.Author.Email,
                When: commit.Author.When,
                ParentShas: commit.Parents.Select(p => p.Sha).ToList(),
                BranchLabels: branchLabels,
                TagLabels: tagLabels,
                Files: files)));
        }
    }

    private bool TryOpenRepo([NotNullWhen(true)] out Repository? repo)
    {
        try
        {
            repo = new Repository(_repoPath);
            return true;
        }
        catch
        {
            repo = null;
            _repoValid = false;
            return false;
        }
    }

    private static void ParseHunkHeader(string line, out int oldStart, out int oldCount, out int newStart, out int newCount)
    {
        oldStart = oldCount = newStart = newCount = 0;
        var match = HunkHeaderRegex().Match(line);
        if (!match.Success)
        {
            return;
        }

        oldStart = int.Parse(match.Groups[1].Value);
        oldCount = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 1;
        newStart = int.Parse(match.Groups[3].Value);
        newCount = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : 1;
    }

    [GeneratedRegex(@"@@ -(\d+)(?:,(\d+))? \+(\d+)(?:,(\d+))? @@")]
    private static partial Regex HunkHeaderRegex();
}
