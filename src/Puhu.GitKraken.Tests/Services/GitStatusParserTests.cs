using Puhu.GitKraken.Services;

namespace Puhu.GitKraken.Tests.Services;

public sealed class GitStatusParserTests
{
    [Fact]
    public Task Staged_modified_and_added()
    {
        var input = "1 M. N... 100644 100644 100644 abc1234 def5678 src/Login.cs\n1 A. N... 000000 100644 100644 0000000 abc1234 src/NewFile.cs";
        var status = GitStatusParser.Parse(input);
        return Verify(status);
    }

    [Fact]
    public Task Unstaged_modified_and_deleted()
    {
        var input = "1 .M N... 100644 100644 100644 abc1234 def5678 src/Login.cs\n1 .D N... 100644 100644 000000 abc1234 0000000 src/OldFile.cs";
        var status = GitStatusParser.Parse(input);
        return Verify(status);
    }

    [Fact]
    public Task Untracked_files()
    {
        var input = "? todo.txt\n? docs/notes.md";
        var status = GitStatusParser.Parse(input);
        return Verify(status);
    }

    [Fact]
    public Task Mixed_staged_unstaged_untracked()
    {
        var input = "1 M. N... 100644 100644 100644 abc1234 def5678 src/Staged.cs\n1 .M N... 100644 100644 100644 abc1234 def5678 src/Unstaged.cs\n? newfile.txt";
        var status = GitStatusParser.Parse(input);
        return Verify(status);
    }

    [Fact]
    public Task Renamed_file()
    {
        var input = "2 R. N... 100644 100644 100644 abc1234 def5678 R100 src/New.cs\tsrc/Old.cs";
        var status = GitStatusParser.Parse(input);
        return Verify(status);
    }

    [Fact]
    public void Empty_input_returns_empty_status()
    {
        var status = GitStatusParser.Parse("");
        Assert.Empty(status.Staged);
        Assert.Empty(status.Unstaged);
        Assert.Empty(status.Untracked);
    }

    [Fact]
    public Task Both_staged_and_unstaged_same_file()
    {
        var input = "1 MM N... 100644 100644 100644 abc1234 def5678 src/Both.cs";
        var status = GitStatusParser.Parse(input);
        return Verify(status);
    }
}
