namespace Puhu.GitKraken.Tests;

public sealed class GitKrakenPluginTests
{
    [Fact]
    public void Plugin_exposes_its_name()
    {
        var plugin = new GitKrakenPlugin();

        Assert.Equal("GitKraken", plugin.Name);
    }
}
