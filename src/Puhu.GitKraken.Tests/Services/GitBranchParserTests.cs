using Puhu.GitKraken.Services;

namespace Puhu.GitKraken.Tests.Services;

public sealed class GitBranchParserTests
{
    [Fact]
    public Task Local_branches_with_head()
    {
        var input = string.Join('\n',
            "main\0abc1234\0*\0origin/main",
            "feature\0def5678\0 \0",
            "bugfix\0ghi9012\0 \0origin/bugfix");
        var branches = GitBranchParser.Parse(input);
        return Verify(branches);
    }

    [Fact]
    public void Empty_input_returns_empty()
    {
        var branches = GitBranchParser.Parse("");
        Assert.Empty(branches);
    }

    [Fact]
    public Task Remotes_from_verbose_output()
    {
        var input = "origin\thttps://github.com/user/repo.git (fetch)\norigin\thttps://github.com/user/repo.git (push)\nupstream\thttps://github.com/other/repo.git (fetch)\nupstream\tgit@github.com:other/repo.git (push)";
        var remotes = GitBranchParser.ParseRemotes(input);
        return Verify(remotes);
    }
}
