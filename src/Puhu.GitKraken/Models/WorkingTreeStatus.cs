namespace Puhu.GitKraken.Models;

public enum FileStatus { Added, Modified, Deleted, Renamed, Copied }

public sealed record StatusEntry(string Path, FileStatus Status, string? OldPath);

public sealed record WorkingTreeStatus(
    IReadOnlyList<StatusEntry> Staged,
    IReadOnlyList<StatusEntry> Unstaged,
    IReadOnlyList<StatusEntry> Untracked);
