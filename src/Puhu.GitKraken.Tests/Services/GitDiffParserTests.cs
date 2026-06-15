using Puhu.GitKraken.Services;

namespace Puhu.GitKraken.Tests.Services;

public sealed class GitDiffParserTests
{
    [Fact]
    public Task Single_file_modification()
    {
        var input = "diff --git a/src/File.cs b/src/File.cs\nindex abc1234..def5678 100644\n--- a/src/File.cs\n+++ b/src/File.cs\n@@ -10,6 +10,7 @@ namespace Example\n     var x = 1;\n     var y = 2;\n+    var z = 3;\n     return x + y;";
        var files = GitDiffParser.Parse(input);
        return Verify(files);
    }

    [Fact]
    public Task New_file()
    {
        var input = "diff --git a/src/New.cs b/src/New.cs\nnew file mode 100644\nindex 0000000..abc1234\n--- /dev/null\n+++ b/src/New.cs\n@@ -0,0 +1,3 @@\n+namespace Example;\n+\n+public class New { }";
        var files = GitDiffParser.Parse(input);
        return Verify(files);
    }

    [Fact]
    public Task Deleted_file()
    {
        var input = "diff --git a/src/Old.cs b/src/Old.cs\ndeleted file mode 100644\nindex abc1234..0000000\n--- a/src/Old.cs\n+++ /dev/null\n@@ -1,3 +0,0 @@\n-namespace Example;\n-\n-public class Old { }";
        var files = GitDiffParser.Parse(input);
        return Verify(files);
    }

    [Fact]
    public Task Renamed_file()
    {
        var input = "diff --git a/src/Old.cs b/src/New.cs\nsimilarity index 95%\nrename from src/Old.cs\nrename to src/New.cs\nindex abc1234..def5678 100644\n--- a/src/Old.cs\n+++ b/src/New.cs\n@@ -1,3 +1,3 @@\n namespace Example;\n \n-public class Old { }\n+public class New { }";
        var files = GitDiffParser.Parse(input);
        return Verify(files);
    }

    [Fact]
    public Task Multiple_files()
    {
        var input = "diff --git a/src/A.cs b/src/A.cs\nindex abc1234..def5678 100644\n--- a/src/A.cs\n+++ b/src/A.cs\n@@ -1,3 +1,4 @@\n line1\n+line2\n line3\ndiff --git a/src/B.cs b/src/B.cs\nnew file mode 100644\nindex 0000000..abc1234\n--- /dev/null\n+++ b/src/B.cs\n@@ -0,0 +1,1 @@\n+content";
        var files = GitDiffParser.Parse(input);
        return Verify(files);
    }

    [Fact]
    public void Empty_input_returns_empty()
    {
        var files = GitDiffParser.Parse("");
        Assert.Empty(files);
    }
}
