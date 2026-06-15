using Puhu.GitKraken.Services;

namespace Puhu.GitKraken.Tests.Services;

public sealed class GitLogParserTests
{
    [Fact]
    public Task Single_commit()
    {
        var input = "abc1234567890abcdef1234567890abcdef123456\0abc1234\0Initial commit\0Test Author\02026-06-15T12:00:00+00:00\0\0HEAD -> main";

        var commits = GitLogParser.Parse(input);

        return Verify(commits);
    }

    [Fact]
    public Task Multiple_commits_with_parents()
    {
        var input = string.Join('\n',
            "aaa1234567890abcdef1234567890abcdef123456\0aaa1234\0Third\0Author\02026-06-15T14:00:00+00:00\0bbb1234567890abcdef1234567890abcdef123456\0HEAD -> main",
            "bbb1234567890abcdef1234567890abcdef123456\0bbb1234\0Second\0Author\02026-06-15T13:00:00+00:00\0ccc1234567890abcdef1234567890abcdef123456\0",
            "ccc1234567890abcdef1234567890abcdef123456\0ccc1234\0First\0Author\02026-06-15T12:00:00+00:00\0\0tag: v1.0");

        var commits = GitLogParser.Parse(input);

        return Verify(commits);
    }

    [Fact]
    public Task Merge_commit_with_two_parents()
    {
        var input = "aaa1234567890abcdef1234567890abcdef123456\0aaa1234\0Merge feature\0Author\02026-06-15T14:00:00+00:00\0bbb1234567890abcdef1234567890abcdef123456 ccc1234567890abcdef1234567890abcdef123456\0HEAD -> main";

        var commits = GitLogParser.Parse(input);

        return Verify(commits);
    }

    [Fact]
    public void Empty_input_returns_empty()
    {
        var commits = GitLogParser.Parse("");
        Assert.Empty(commits);
    }

    [Fact]
    public Task Refs_with_multiple_branches_and_tags()
    {
        var input = "aaa1234567890abcdef1234567890abcdef123456\0aaa1234\0Latest\0Author\02026-06-15T14:00:00+00:00\0\0HEAD -> main, origin/main, tag: v2.0, tag: release";

        var commits = GitLogParser.Parse(input);

        return Verify(commits);
    }
}
