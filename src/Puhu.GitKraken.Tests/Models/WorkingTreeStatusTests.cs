using Puhu.GitKraken.Models;

namespace Puhu.GitKraken.Tests.Models;

public sealed class WorkingTreeStatusTests
{
    [Fact]
    public void StatusEntry_sets_properties()
    {
        var entry = new StatusEntry("src/File.cs", FileStatus.Modified, null);
        Assert.Equal("src/File.cs", entry.Path);
        Assert.Equal(FileStatus.Modified, entry.Status);
        Assert.Null(entry.OldPath);
    }

    [Fact]
    public void WorkingTreeStatus_groups_entries()
    {
        var status = new WorkingTreeStatus(
            Staged: [new StatusEntry("a.cs", FileStatus.Added, null)],
            Unstaged: [new StatusEntry("b.cs", FileStatus.Modified, null)],
            Untracked: [new StatusEntry("c.txt", FileStatus.Added, null)]);
        Assert.Single(status.Staged);
        Assert.Single(status.Unstaged);
        Assert.Single(status.Untracked);
    }

    [Fact]
    public void FileStatus_has_all_values()
    {
        var values = Enum.GetValues<FileStatus>();
        Assert.Contains(FileStatus.Added, values);
        Assert.Contains(FileStatus.Modified, values);
        Assert.Contains(FileStatus.Deleted, values);
        Assert.Contains(FileStatus.Renamed, values);
        Assert.Contains(FileStatus.Copied, values);
    }
}
