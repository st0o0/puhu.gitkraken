using Puhu.GitKraken.Models;

namespace Puhu.GitKraken.Services;

public static class GitBranchParser
{
    public static IReadOnlyList<BranchInfo> Parse(string output, char fieldSeparator = '\0')
    {
        if (string.IsNullOrWhiteSpace(output))
            return [];

        var branches = new List<BranchInfo>();

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var fields = line.Split(fieldSeparator);
            if (fields.Length < 4)
                continue;

            var name = fields[0];
            var tipSha = fields[1];
            var isHead = fields[2] == "*";
            var tracking = string.IsNullOrEmpty(fields[3]) ? null : fields[3];
            var isRemote = name.Contains('/');

            branches.Add(new BranchInfo(name, isHead, isRemote, tracking, tipSha, null));
        }

        return branches;
    }

    public static IReadOnlyList<RemoteInfo> ParseRemotes(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return [];

        var remotes = new Dictionary<string, (string? Fetch, string? Push)>();

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('\t', 2);
            if (parts.Length < 2) continue;

            var name = parts[0];
            var urlAndType = parts[1];

            if (!remotes.ContainsKey(name))
                remotes[name] = (null, null);

            var current = remotes[name];

            if (urlAndType.EndsWith("(fetch)"))
                remotes[name] = (urlAndType[..^7].Trim(), current.Push);
            else if (urlAndType.EndsWith("(push)"))
                remotes[name] = (current.Fetch, urlAndType[..^6].Trim());
        }

        return remotes.Select(r =>
            new RemoteInfo(r.Key, r.Value.Fetch ?? "", r.Value.Push ?? "")).ToList();
    }
}
