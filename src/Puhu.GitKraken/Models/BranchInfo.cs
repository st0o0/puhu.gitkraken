namespace Puhu.GitKraken.Models;

public sealed record BranchInfo(
    string Name,
    bool IsHead,
    bool IsRemote,
    string? TrackingBranch,
    string? TipSha,
    string? TipMessage);

public sealed record RemoteInfo(string Name, string FetchUrl, string PushUrl);

public enum ResultKind { Success, Conflict, Error }
