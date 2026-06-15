using Puhu.GitKraken.Models;

namespace Puhu.GitKraken.Services;

public static class GitStatusParser
{
    public static WorkingTreeStatus Parse(string output)
    {
        var staged = new List<StatusEntry>();
        var unstaged = new List<StatusEntry>();
        var untracked = new List<StatusEntry>();

        if (string.IsNullOrWhiteSpace(output))
            return new WorkingTreeStatus(staged, unstaged, untracked);

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.TrimStart();

            if (trimmed.StartsWith('?'))
            {
                var path = trimmed[2..];
                untracked.Add(new StatusEntry(path, FileStatus.Added, null));
                continue;
            }

            if (trimmed.StartsWith('1'))
                ParseOrdinaryEntry(trimmed, staged, unstaged);
            else if (trimmed.StartsWith('2'))
                ParseRenamedEntry(trimmed, staged, unstaged);
        }

        return new WorkingTreeStatus(staged, unstaged, untracked);
    }

    private static void ParseOrdinaryEntry(string line, List<StatusEntry> staged, List<StatusEntry> unstaged)
    {
        var parts = line.Split(' ', 9);
        if (parts.Length < 9) return;

        var xy = parts[1];
        var path = parts[8];

        if (xy[0] is not '.')
            staged.Add(new StatusEntry(path, ParseStatusChar(xy[0]), null));
        if (xy[1] is not '.')
            unstaged.Add(new StatusEntry(path, ParseStatusChar(xy[1]), null));
    }

    private static void ParseRenamedEntry(string line, List<StatusEntry> staged, List<StatusEntry> unstaged)
    {
        var parts = line.Split(' ', 10);
        if (parts.Length < 10) return;

        var xy = parts[1];
        var pathParts = parts[9].Split('\t');
        var path = pathParts[0];
        var oldPath = pathParts.Length > 1 ? pathParts[1] : null;

        if (xy[0] is 'R')
            staged.Add(new StatusEntry(path, FileStatus.Renamed, oldPath));
        else if (xy[0] is 'C')
            staged.Add(new StatusEntry(path, FileStatus.Copied, oldPath));

        if (xy[1] is not '.')
            unstaged.Add(new StatusEntry(path, ParseStatusChar(xy[1]), null));
    }

    private static FileStatus ParseStatusChar(char c) => c switch
    {
        'A' => FileStatus.Added,
        'M' => FileStatus.Modified,
        'D' => FileStatus.Deleted,
        'R' => FileStatus.Renamed,
        'C' => FileStatus.Copied,
        _ => FileStatus.Modified,
    };
}
