using Puhu.GitKraken.Models;

namespace Puhu.GitKraken.Actors;

// === Shared ===
public sealed record RefreshRequest;
public sealed record RepoNotFound(string Path);
public sealed record OperationResult(bool Success, string? Error);

// === GitRepoActor ===
public sealed record GetGraph(int MaxCount = 200);
public sealed record GetCommitDetail(string Hash);
public sealed record GraphResponse(IReadOnlyList<GraphCommit> Commits, string? CurrentBranch);
public sealed record CommitDetailResponse(CommitDetail Detail);

// === GitStagingActor ===
public sealed record GetStatus;
public sealed record StatusResponse(WorkingTreeStatus Status);
public sealed record StageFiles(IReadOnlyList<string> Paths);
public sealed record UnstageFiles(IReadOnlyList<string> Paths);
public sealed record StageAll;
public sealed record UnstageAll;
public sealed record GetFileDiff(string Path, bool Staged);
public sealed record FileDiffResponse(FileDiff Diff);

// === GitWriteActor ===
public sealed record CreateCommit(string Message, bool Amend = false);
public sealed record CommitResult(bool Success, string? Sha, string? Error);
public sealed record CreateBranch(string Name, string? StartPoint = null);
public sealed record SwitchBranch(string Name);
public sealed record DeleteBranch(string Name, bool Force = false);
public sealed record RenameBranch(string OldName, string NewName);
public sealed record GetBranches;
public sealed record BranchesResponse(IReadOnlyList<BranchInfo> Branches);
public sealed record MergeBranch(string Name);
public sealed record RebaseBranch(string Onto);
public sealed record AbortMerge;
public sealed record AbortRebase;
public sealed record MergeRebaseResult(ResultKind Kind, string? Error, int? ConflictCount);
public sealed record StashChanges(string? Message = null);
public sealed record StashPop;
public sealed record StashResult(bool Success, string? Error);

// === GitRemoteActor ===
public sealed record GitPush(string? Remote = null, string? Branch = null);
public sealed record GitPull(string? Remote = null, string? Branch = null);
public sealed record GitFetch(string? Remote = null);
public sealed record GetRemotes;
public sealed record RemotesResponse(IReadOnlyList<RemoteInfo> Remotes);
public sealed record RemoteOperationResult(bool Success, string Output, string? Error);
