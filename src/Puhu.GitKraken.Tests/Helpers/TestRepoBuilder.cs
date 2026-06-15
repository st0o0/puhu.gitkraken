using System.Diagnostics;

namespace Puhu.GitKraken.Tests.Helpers;

internal sealed class TestRepoBuilder : IDisposable
{
    private readonly string _path;

    public TestRepoBuilder()
    {
        _path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "puhu-test-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_path);
        Git("init");
        Git("config user.email \"test@example.com\"");
        Git("config user.name \"Test Author\"");
    }

    public string Path => _path;

    public string AddCommit(string message, string fileName = "file.txt", string content = "content")
    {
        var filePath = System.IO.Path.Combine(_path, fileName);
        var dir = System.IO.Path.GetDirectoryName(filePath);
        if (dir is not null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(filePath, content + "\n" + Guid.NewGuid());
        Git($"add \"{fileName}\"");
        Git($"commit -m \"{message}\"");
        return Git("rev-parse HEAD").Trim();
    }

    public void CreateBranch(string name) => Git($"branch \"{name}\"");

    public void Checkout(string branchName) => Git($"checkout \"{branchName}\"");

    public string MergeBranch(string branchName, string message)
    {
        Git($"merge \"{branchName}\" --no-edit -m \"{message}\"");
        return Git("rev-parse HEAD").Trim();
    }

    public void WriteFile(string fileName, string content)
    {
        var filePath = System.IO.Path.Combine(_path, fileName);
        var dir = System.IO.Path.GetDirectoryName(filePath);
        if (dir is not null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(filePath, content);
    }

    public string Git(string args)
    {
        var psi = new ProcessStartInfo("git", args)
        {
            WorkingDirectory = _path,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            Environment = { ["LC_ALL"] = "C" },
        };

        using var process = Process.Start(psi)!;
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"git {args} failed (exit {process.ExitCode}): {stderr}");

        return stdout;
    }

    public void Dispose()
    {
        try
        {
            foreach (var file in Directory.EnumerateFiles(_path, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }
            Directory.Delete(_path, true);
        }
        catch { /* best effort cleanup */ }
    }
}
