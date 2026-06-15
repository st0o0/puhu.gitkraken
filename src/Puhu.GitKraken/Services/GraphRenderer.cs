namespace Puhu.GitKraken.Services;

public sealed record RenderedGraphRow(
    string GraphPrefix,
    GraphCommit Commit,
    int ActiveColumns);

public sealed class GraphRenderer
{
    public IReadOnlyList<RenderedGraphRow> Render(IReadOnlyList<GraphCommit> commits)
    {
        if (commits.Count == 0)
            return [];

        var rows = new List<RenderedGraphRow>(commits.Count);
        var columns = new List<string?>();

        foreach (var commit in commits)
        {
            var col = FindOrAddColumn(columns, commit.FullSha);
            var prefix = BuildPrefix(columns, col);
            rows.Add(new RenderedGraphRow(prefix, commit, CountActive(columns)));

            AdvanceColumns(columns, col, commit);
        }

        return rows;
    }

    private static int FindOrAddColumn(List<string?> columns, string sha)
    {
        for (var i = 0; i < columns.Count; i++)
        {
            if (columns[i] == sha)
                return i;
        }

        var slot = columns.IndexOf(null);
        if (slot >= 0)
        {
            columns[slot] = sha;
            return slot;
        }

        columns.Add(sha);
        return columns.Count - 1;
    }

    private static string BuildPrefix(List<string?> columns, int commitCol)
    {
        var chars = new char[columns.Count * 2];

        for (var i = 0; i < columns.Count; i++)
        {
            if (i == commitCol)
            {
                chars[i * 2] = '●';
                chars[i * 2 + 1] = ' ';
            }
            else if (columns[i] is not null)
            {
                chars[i * 2] = '│';
                chars[i * 2 + 1] = ' ';
            }
            else
            {
                chars[i * 2] = ' ';
                chars[i * 2 + 1] = ' ';
            }
        }

        return new string(chars);
    }

    private static void AdvanceColumns(List<string?> columns, int commitCol, GraphCommit commit)
    {
        if (commit.ParentShas.Count == 0)
        {
            columns[commitCol] = null;
        }
        else
        {
            columns[commitCol] = commit.ParentShas[0];

            for (var p = 1; p < commit.ParentShas.Count; p++)
            {
                var parentSha = commit.ParentShas[p];
                var existing = columns.IndexOf(parentSha);
                if (existing < 0)
                {
                    var slot = -1;
                    for (var i = 0; i < columns.Count; i++)
                    {
                        if (i != commitCol && columns[i] is null)
                        {
                            slot = i;
                            break;
                        }
                    }

                    if (slot >= 0)
                        columns[slot] = parentSha;
                    else
                        columns.Add(parentSha);
                }
            }
        }
    }

    private static int CountActive(List<string?> columns) =>
        columns.Count(c => c is not null);
}
