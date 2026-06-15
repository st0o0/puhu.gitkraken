using Puhu.GitKraken.Models;

namespace Puhu.GitKraken.Tests.Models;

public sealed class BranchInfoTests
{
    [Fact]
    public void BranchInfo_sets_properties()
    {
        var branch = new BranchInfo("main", true, false, "origin/main", "abc1234", "Latest commit");
        Assert.Equal("main", branch.Name);
        Assert.True(branch.IsHead);
        Assert.False(branch.IsRemote);
        Assert.Equal("origin/main", branch.TrackingBranch);
    }

    [Fact]
    public void RemoteInfo_sets_properties()
    {
        var remote = new RemoteInfo("origin", "https://github.com/user/repo.git", "https://github.com/user/repo.git");
        Assert.Equal("origin", remote.Name);
        Assert.Equal("https://github.com/user/repo.git", remote.FetchUrl);
    }

    [Fact]
    public void ResultKind_has_all_values()
    {
        var values = Enum.GetValues<ResultKind>();
        Assert.Contains(ResultKind.Success, values);
        Assert.Contains(ResultKind.Conflict, values);
        Assert.Contains(ResultKind.Error, values);
    }
}
