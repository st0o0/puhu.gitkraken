using System.Text.RegularExpressions;
using Puhu.GitKraken.Models;

namespace Puhu.GitKraken.Services;

public static partial class GitDiffParser
{
    public static IReadOnlyList<FileDiff> Parse(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return [];

        var files = new List<FileDiff>();
        var lines = output.Split('\n');
        var i = 0;

        while (i < lines.Length)
        {
            var line = lines[i];

            if (!line.StartsWith("diff --git "))
            {
                i++;
                continue;
            }

            var diffHeaderMatch = DiffHeaderRegex().Match(line);
            if (!diffHeaderMatch.Success)
            {
                i++;
                continue;
            }

            var pathA = diffHeaderMatch.Groups[1].Value;
            var pathB = diffHeaderMatch.Groups[2].Value;
            i++;

            var kind = ChangeKind.Modified;
            string? oldPath = null;

            while (i < lines.Length && !lines[i].StartsWith("--- ") && !lines[i].StartsWith("diff --git "))
            {
                if (lines[i].StartsWith("new file"))
                    kind = ChangeKind.Added;
                else if (lines[i].StartsWith("deleted file"))
                    kind = ChangeKind.Deleted;
                else if (lines[i].StartsWith("rename from "))
                {
                    kind = ChangeKind.Renamed;
                    oldPath = lines[i][12..];
                }
                i++;
            }

            if (i < lines.Length && lines[i].StartsWith("--- ")) i++;
            if (i < lines.Length && lines[i].StartsWith("+++ ")) i++;

            var hunks = new List<DiffHunk>();

            while (i < lines.Length && !lines[i].StartsWith("diff --git "))
            {
                if (lines[i].StartsWith("@@"))
                {
                    var hunkMatch = HunkHeaderRegex().Match(lines[i]);
                    if (hunkMatch.Success)
                    {
                        var oldStart = int.Parse(hunkMatch.Groups[1].Value);
                        var oldCount = hunkMatch.Groups[2].Success ? int.Parse(hunkMatch.Groups[2].Value) : 1;
                        var newStart = int.Parse(hunkMatch.Groups[3].Value);
                        var newCount = hunkMatch.Groups[4].Success ? int.Parse(hunkMatch.Groups[4].Value) : 1;
                        i++;

                        var hunkLines = new List<DiffLine>();
                        while (i < lines.Length && !lines[i].StartsWith("@@") && !lines[i].StartsWith("diff --git "))
                        {
                            var hunkLine = lines[i];
                            if (hunkLine.StartsWith('+'))
                                hunkLines.Add(new DiffLine(DiffLineKind.Added, hunkLine[1..]));
                            else if (hunkLine.StartsWith('-'))
                                hunkLines.Add(new DiffLine(DiffLineKind.Removed, hunkLine[1..]));
                            else if (hunkLine.StartsWith(' '))
                                hunkLines.Add(new DiffLine(DiffLineKind.Context, hunkLine[1..]));
                            i++;
                        }

                        hunks.Add(new DiffHunk(oldStart, oldCount, newStart, newCount, hunkLines));
                    }
                    else
                    {
                        i++;
                    }
                }
                else
                {
                    i++;
                }
            }

            files.Add(new FileDiff(pathB, kind, oldPath, hunks));
        }

        return files;
    }

    [GeneratedRegex(@"^diff --git a/(.+) b/(.+)$")]
    private static partial Regex DiffHeaderRegex();

    [GeneratedRegex(@"@@ -(\d+)(?:,(\d+))? \+(\d+)(?:,(\d+))? @@")]
    private static partial Regex HunkHeaderRegex();
}
