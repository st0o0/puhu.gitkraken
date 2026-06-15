using Puhu.GitKraken.Models;
using Puhu.Plugin;
using Termina.Layout;

namespace Puhu.GitKraken.Pages;

public static class StatusPanelBuilder
{
    public static ILayoutNode Build(
        WorkingTreeStatus status,
        ThemeDefinition theme,
        int selectedIndex,
        bool isFocused)
    {
        var lines = new List<ILayoutNode>();
        var currentIndex = 0;

        if (status.Staged.Count > 0)
        {
            lines.Add(new TextNode($" Staged ({status.Staged.Count})")
                .WithForeground(theme.PanelTitle).Bold().Height(1));
            foreach (var entry in status.Staged)
            {
                var isSelected = isFocused && currentIndex == selectedIndex;
                lines.Add(BuildFileEntry(entry, theme, isSelected));
                currentIndex++;
            }
        }

        if (status.Unstaged.Count > 0)
        {
            if (lines.Count > 0) lines.Add(Layouts.Empty().Height(1));
            lines.Add(new TextNode($" Unstaged ({status.Unstaged.Count})")
                .WithForeground(theme.PanelTitle).Bold().Height(1));
            foreach (var entry in status.Unstaged)
            {
                var isSelected = isFocused && currentIndex == selectedIndex;
                lines.Add(BuildFileEntry(entry, theme, isSelected));
                currentIndex++;
            }
        }

        if (status.Untracked.Count > 0)
        {
            if (lines.Count > 0) lines.Add(Layouts.Empty().Height(1));
            lines.Add(new TextNode($" Untracked ({status.Untracked.Count})")
                .WithForeground(theme.PanelTitle).Bold().Height(1));
            foreach (var entry in status.Untracked)
            {
                var isSelected = isFocused && currentIndex == selectedIndex;
                lines.Add(BuildUntrackedEntry(entry, theme, isSelected));
                currentIndex++;
            }
        }

        if (lines.Count == 0)
            lines.Add(new TextNode(" Working tree clean").WithForeground(theme.TextDim).Height(1));

        return Layouts.Vertical(lines.ToArray());
    }

    public static int GetTotalEntries(WorkingTreeStatus status) =>
        status.Staged.Count + status.Unstaged.Count + status.Untracked.Count;

    public static (StatusSection Section, int IndexInSection) GetSectionForIndex(WorkingTreeStatus status, int index)
    {
        if (index < status.Staged.Count)
            return (StatusSection.Staged, index);
        index -= status.Staged.Count;
        if (index < status.Unstaged.Count)
            return (StatusSection.Unstaged, index);
        index -= status.Unstaged.Count;
        return (StatusSection.Untracked, index);
    }

    public static StatusEntry? GetEntryAtIndex(WorkingTreeStatus status, int index)
    {
        if (index < 0) return null;
        if (index < status.Staged.Count) return status.Staged[index];
        index -= status.Staged.Count;
        if (index < status.Unstaged.Count) return status.Unstaged[index];
        index -= status.Unstaged.Count;
        if (index < status.Untracked.Count) return status.Untracked[index];
        return null;
    }

    private static ILayoutNode BuildFileEntry(StatusEntry entry, ThemeDefinition theme, bool isSelected)
    {
        var statusChar = entry.Status switch
        {
            FileStatus.Added => "A", FileStatus.Modified => "M",
            FileStatus.Deleted => "D", FileStatus.Renamed => "R",
            FileStatus.Copied => "C", _ => "?"
        };
        var color = entry.Status switch
        {
            FileStatus.Added => theme.Success, FileStatus.Deleted => theme.Error,
            _ => theme.Foreground,
        };
        var fg = isSelected ? theme.SelectionText : color;
        var bg = isSelected ? theme.Selection : theme.Background;
        return new TextNode($"  {statusChar} {entry.Path}").WithForeground(fg).WithBackground(bg).Height(1);
    }

    private static ILayoutNode BuildUntrackedEntry(StatusEntry entry, ThemeDefinition theme, bool isSelected)
    {
        var fg = isSelected ? theme.SelectionText : theme.TextDim;
        var bg = isSelected ? theme.Selection : theme.Background;
        return new TextNode($"  ? {entry.Path}").WithForeground(fg).WithBackground(bg).Height(1);
    }
}

public enum StatusSection { Staged, Unstaged, Untracked }
