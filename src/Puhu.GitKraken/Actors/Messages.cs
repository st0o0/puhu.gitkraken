namespace Puhu.GitKraken.Actors;

public sealed record RefreshRequest;

public sealed record GetGraph(int MaxCount = 200);

public sealed record GetCommitDetail(string Hash);

public sealed record GraphResponse(IReadOnlyList<GraphCommit> Commits, string? CurrentBranch);

public sealed record CommitDetailResponse(CommitDetail Detail);

public sealed record RepoNotFound(string Path);
