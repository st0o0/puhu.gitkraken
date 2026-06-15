using Puhu.GitKraken.Services;
using Puhu.GitKraken.Tests.Helpers;

namespace Puhu.GitKraken.Tests.Services;

public sealed class GitCliServiceTests : IDisposable
{
    private readonly TestRepoBuilder _repo = new();
    private readonly GitCliService _cli;

    public GitCliServiceTests()
    {
        _cli = new GitCliService(new GitRepoSettings(_repo.Path));
    }

    [Fact]
    public async Task RunAsync_returns_stdout_on_success()
    {
        _repo.AddCommit("Initial commit");

        var result = await _cli.RunAsync("rev-parse HEAD", ct: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(40, result.Stdout.Trim().Length);
        Assert.Empty(result.Stderr);
    }

    [Fact]
    public async Task RunAsync_returns_stderr_on_failure()
    {
        var result = await _cli.RunAsync("checkout nonexistent-branch", ct: TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.NotEmpty(result.Stderr);
    }

    [Fact]
    public async Task RunAsync_respects_timeout()
    {
        var result = await _cli.RunAsync("version", timeout: TimeSpan.FromSeconds(10), ct: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Contains("git version", result.Stdout);
    }

    [Fact]
    public async Task RunAsync_uses_repo_working_directory()
    {
        _repo.AddCommit("Test commit");

        var result = await _cli.RunAsync("log --oneline -1", ct: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Contains("Test commit", result.Stdout);
    }

    public void Dispose() => _repo.Dispose();
}
