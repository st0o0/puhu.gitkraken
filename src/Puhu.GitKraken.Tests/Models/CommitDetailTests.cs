namespace Puhu.GitKraken.Tests.Models;

public sealed class CommitDetailTests
{
    [Fact]
    public void CommitDetail_SetsAllProperties()
    {
        var when = new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var hunks = new List<DiffHunk>
        {
            new(OldStart: 15, OldCount: 8, NewStart: 15, NewCount: 19, Lines:
            [
                new DiffLine(DiffLineKind.Context, "  var x = 1;"),
                new DiffLine(DiffLineKind.Removed, "  // TODO: fix"),
                new DiffLine(DiffLineKind.Added, "  if (user.IsValid())"),
            ])
        };

        var files = new List<FileDiff>
        {
            new(Path: "src/Login.cs", Kind: ChangeKind.Modified, OldPath: null, Hunks: hunks)
        };

        var detail = new CommitDetail(
            Sha: "a1b2c3d",
            Message: "Fix login bug\n\nDetailed description here.",
            AuthorName: "st0o0",
            AuthorEmail: "claude@schloots.net",
            When: when,
            ParentShas: ["f8e9d0c"],
            BranchLabels: ["main"],
            TagLabels: [],
            Files: files);

        Assert.Equal("a1b2c3d", detail.Sha);
        Assert.Contains("\n", detail.Message);
        Assert.Equal("claude@schloots.net", detail.AuthorEmail);
        Assert.Single(detail.Files);
        Assert.Equal(ChangeKind.Modified, detail.Files[0].Kind);
        Assert.Single(detail.Files[0].Hunks);
        Assert.Equal(3, detail.Files[0].Hunks[0].Lines.Count);
    }

    [Fact]
    public void FileDiff_Renamed_HasOldPath()
    {
        var file = new FileDiff(
            Path: "src/Auth.cs",
            Kind: ChangeKind.Renamed,
            OldPath: "src/OldAuth.cs",
            Hunks: []);

        Assert.Equal("src/OldAuth.cs", file.OldPath);
        Assert.Equal(ChangeKind.Renamed, file.Kind);
    }

    [Fact]
    public void DiffLine_KindValues_AreDistinct()
    {
        Assert.NotEqual(DiffLineKind.Added, DiffLineKind.Removed);
        Assert.NotEqual(DiffLineKind.Context, DiffLineKind.Added);
    }

    [Fact]
    public void ChangeKind_HasAllExpectedValues()
    {
        var values = Enum.GetValues<ChangeKind>();
        Assert.Contains(ChangeKind.Added, values);
        Assert.Contains(ChangeKind.Modified, values);
        Assert.Contains(ChangeKind.Deleted, values);
        Assert.Contains(ChangeKind.Renamed, values);
    }
}
