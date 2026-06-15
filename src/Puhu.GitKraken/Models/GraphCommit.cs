namespace Puhu.GitKraken;

public sealed record GraphCommit(
    string Sha,
    string FullSha,
    string MessageShort,
    string AuthorName,
    DateTimeOffset When,
    IReadOnlyList<string> ParentShas,
    IReadOnlyList<string> BranchLabels,
    IReadOnlyList<string> TagLabels);
