using Puhu.GitKraken.Models;

namespace Puhu.GitKraken.Tests.Models;

public sealed class GraphCommitTests
{
    [Fact]
    public void Ctor_SetsAllProperties()
    {
        var when = new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var commit = new GraphCommit(
            Sha: "a1b2c3d",
            FullSha: "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2",
            MessageShort: "Fix login bug",
            AuthorName: "st0o0",
            When: when,
            ParentShas: ["f8e9d0c"],
            BranchLabels: ["main", "origin/main"],
            TagLabels: ["v1.0"]);

        Assert.Equal("a1b2c3d", commit.Sha);
        Assert.Equal("Fix login bug", commit.MessageShort);
        Assert.Equal("st0o0", commit.AuthorName);
        Assert.Equal(when, commit.When);
        Assert.Single(commit.ParentShas);
        Assert.Equal(2, commit.BranchLabels.Count);
        Assert.Single(commit.TagLabels);
    }

    [Fact]
    public void Record_equality_works()
    {
        var when = DateTimeOffset.UtcNow;
        var a = new GraphCommit("abc", "abcdef", "msg", "author", when, [], [], []);
        var b = new GraphCommit("abc", "abcdef", "msg", "author", when, [], [], []);

        Assert.Equal(a, b);
    }
}
