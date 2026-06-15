using System.Diagnostics;

namespace Puhu.GitKraken.Services;

public sealed record GitResult(int ExitCode, string Stdout, string Stderr)
{
    public bool Success => ExitCode == 0;
}

public sealed class GitCliService(GitRepoSettings settings)
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    public string RepoPath => settings.RepoPath;

    public async Task<GitResult> RunAsync(string args, TimeSpan? timeout = null, CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo("git", $"--no-pager {args}")
        {
            WorkingDirectory = settings.RepoPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            Environment = { ["LC_ALL"] = "C" },
        };

        using var process = new Process { StartInfo = psi };
        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            return new GitResult(-1, "", ex.Message);
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(timeout ?? DefaultTimeout);

        try
        {
            var stdoutTask = process.StandardOutput.ReadToEndAsync(timeoutCts.Token);
            var stderrTask = process.StandardError.ReadToEndAsync(timeoutCts.Token);

            await process.WaitForExitAsync(timeoutCts.Token);

            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            return new GitResult(process.ExitCode, stdout, stderr);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            return new GitResult(-1, "", "Operation timed out");
        }
    }
}
