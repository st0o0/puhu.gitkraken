using Puhu.GitKraken.Models;

namespace Puhu.GitKraken.Services;

public static class GitLogParser
{
    private const char FieldSeparator = '\0';

    public static IReadOnlyList<GraphCommit> Parse(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return [];

        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var commits = new List<GraphCommit>(lines.Length);

        foreach (var line in lines)
        {
            var fields = line.Split(FieldSeparator);
            if (fields.Length < 7)
                continue;

            var fullSha = fields[0];
            var sha = fields[1];
            var messageShort = fields[2];
            var authorName = fields[3];
            var when = DateTimeOffset.Parse(fields[4]);
            var parentShas = string.IsNullOrEmpty(fields[5])
                ? []
                : fields[5].Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
            var refs = ParseRefs(fields[6]);

            commits.Add(new GraphCommit(
                Sha: sha,
                FullSha: fullSha,
                MessageShort: messageShort,
                AuthorName: authorName,
                When: when,
                ParentShas: parentShas,
                BranchLabels: refs.Branches,
                TagLabels: refs.Tags));
        }

        return commits;
    }

    private static (List<string> Branches, List<string> Tags) ParseRefs(string refString)
    {
        var branches = new List<string>();
        var tags = new List<string>();

        if (string.IsNullOrWhiteSpace(refString))
            return (branches, tags);

        foreach (var r in refString.Split(',', StringSplitOptions.TrimEntries))
        {
            if (r.StartsWith("tag: "))
                tags.Add(r[5..]);
            else if (r.StartsWith("HEAD -> "))
                branches.Add(r[8..]);
            else if (r is not "HEAD")
                branches.Add(r);
        }

        return (branches, tags);
    }
}
