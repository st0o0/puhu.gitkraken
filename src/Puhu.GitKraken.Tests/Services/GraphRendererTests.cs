using Puhu.GitKraken.Models;
using Puhu.GitKraken.Services;

namespace Puhu.GitKraken.Tests.Services;

public sealed class GraphRendererTests
{
    private const string ShaPadding = "0000000000000000000000000000000000";

    private static readonly DateTimeOffset BaseTime =
        new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public Task Single_commit()
    {
        var commits = new List<GraphCommit>
        {
            Commit("a1b2c3d", message: "Initial commit"),
        };

        var renderer = new GraphRenderer();
        var rows = renderer.Render(commits);

        return Verify(FormatRows(rows));
    }

    [Fact]
    public Task Linear_history_three_commits()
    {
        var commits = new List<GraphCommit>
        {
            Commit("a1b2c3d", message: "Third", parents: ["b2c3d4e"]),
            Commit("b2c3d4e", message: "Second", parents: ["c3d4e5f"]),
            Commit("c3d4e5f", message: "First"),
        };

        var renderer = new GraphRenderer();
        var rows = renderer.Render(commits);

        return Verify(FormatRows(rows));
    }

    [Fact]
    public Task Branch_and_merge()
    {
        var commits = new List<GraphCommit>
        {
            Commit("aaa", message: "Merge branch 'feature'", parents: ["bbb", "ccc"]),
            Commit("bbb", message: "Main work", parents: ["ddd"]),
            Commit("ccc", message: "Feature work", parents: ["ddd"]),
            Commit("ddd", message: "Common ancestor"),
        };

        var renderer = new GraphRenderer();
        var rows = renderer.Render(commits);

        return Verify(FormatRows(rows));
    }

    [Fact]
    public Task Parallel_branches()
    {
        var commits = new List<GraphCommit>
        {
            Commit("aaa", message: "Latest on main", parents: ["bbb"]),
            Commit("bbb", message: "Merge feature-a", parents: ["ddd", "ccc"]),
            Commit("ccc", message: "Work on feature-a", parents: ["eee"]),
            Commit("ddd", message: "Merge feature-b", parents: ["eee", "fff"]),
            Commit("fff", message: "Work on feature-b", parents: ["eee"]),
            Commit("eee", message: "Root"),
        };

        var renderer = new GraphRenderer();
        var rows = renderer.Render(commits);

        return Verify(FormatRows(rows));
    }

    [Fact]
    public void Empty_input_returns_empty()
    {
        var renderer = new GraphRenderer();
        var rows = renderer.Render([]);

        Assert.Empty(rows);
    }

    private static GraphCommit Commit(
        string sha,
        string message = "msg",
        string[]? parents = null,
        string[]? branches = null,
        string[]? tags = null) =>
        new(sha, sha + ShaPadding,
            message, "author", BaseTime,
            parents?.Select(p => p + ShaPadding).ToArray() ?? [],
            branches ?? [],
            tags ?? []);

    private static string FormatRows(IReadOnlyList<RenderedGraphRow> rows) =>
        string.Join('\n', rows.Select(r => $"{r.GraphPrefix}{r.Commit.MessageShort}"));
}
