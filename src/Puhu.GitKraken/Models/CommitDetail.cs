namespace Puhu.GitKraken;

public enum ChangeKind { Added, Modified, Deleted, Renamed }

public enum DiffLineKind { Context, Added, Removed }

public sealed record CommitDetail(
    string Sha,
    string Message,
    string AuthorName,
    string AuthorEmail,
    DateTimeOffset When,
    IReadOnlyList<string> ParentShas,
    IReadOnlyList<string> BranchLabels,
    IReadOnlyList<string> TagLabels,
    IReadOnlyList<FileDiff> Files);

public sealed record FileDiff(
    string Path,
    ChangeKind Kind,
    string? OldPath,
    IReadOnlyList<DiffHunk> Hunks);

public sealed record DiffHunk(
    int OldStart,
    int OldCount,
    int NewStart,
    int NewCount,
    IReadOnlyList<DiffLine> Lines);

public sealed record DiffLine(
    DiffLineKind Kind,
    string Content);
