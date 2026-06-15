using LibGit2Sharp;

namespace Puhu.GitKraken.Tests.Helpers;

internal sealed class TestRepoBuilder : IDisposable
{
    private readonly string _path;
    public Repository Repo { get; }

    public TestRepoBuilder()
    {
        _path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "puhu-test-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_path);
        Repository.Init(_path);
        Repo = new Repository(_path);
    }

    public string Path => _path;

    public Commit AddCommit(string message, string fileName = "file.txt", string content = "content")
    {
        var filePath = System.IO.Path.Combine(_path, fileName);
        File.WriteAllText(filePath, content + "\n" + Guid.NewGuid());
        Commands.Stage(Repo, fileName);

        var author = new Signature("Test Author", "test@example.com", DateTimeOffset.UtcNow);
        return Repo.Commit(message, author, author);
    }

    public Branch CreateBranch(string name) => Repo.CreateBranch(name);

    public void Checkout(string branchName)
    {
        var branch = Repo.Branches[branchName];
        Commands.Checkout(Repo, branch);
    }

    public Commit MergeBranch(string branchName, string message)
    {
        var branch = Repo.Branches[branchName];
        var mergeResult = Repo.Merge(branch, Repo.Config.BuildSignature(DateTimeOffset.UtcNow));

        if (mergeResult.Status == MergeStatus.UpToDate || mergeResult.Status == MergeStatus.FastForward)
        {
            return mergeResult.Commit;
        }

        return mergeResult.Commit;
    }

    public void Dispose()
    {
        Repo.Dispose();
        try { Directory.Delete(_path, true); }
        catch { /* best effort cleanup */ }
    }
}
