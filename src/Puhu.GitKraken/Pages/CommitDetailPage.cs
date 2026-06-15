using Puhu.Plugin;
using R3;
using Termina.Layout;
using Termina.Reactive;

namespace Puhu.GitKraken.Pages;

public sealed class CommitDetailPage : ReactivePage<CommitDetailViewModel>, IKeyHintProvider
{
    private readonly IThemeService _themeService;

    public CommitDetailPage(IThemeService themeService)
    {
        _themeService = themeService;
    }

    public string[] GetKeyHints() =>
        ["Esc:Back", "Tab:Next File", "Enter:Toggle", "Up/Dn:Scroll"];

    public override ILayoutNode BuildLayout()
    {
        var theme = _themeService.Current;
        var detail = ViewModel.Detail.Value;

        if (detail is null)
        {
            return new TextNode(ViewModel.StatusMessage.Value)
                .WithForeground(theme.Warning)
                .AlignCenter()
                .Fill();
        }

        var lines = new List<ILayoutNode>();

        // Commit header
        lines.Add(new TextNode($" Commit {detail.Sha}").WithForeground(theme.Accent).Height(1));
        lines.Add(new TextNode($" {detail.Message}").WithForeground(theme.Foreground).Height(1));
        lines.Add(Layouts.Empty().Height(1));
        lines.Add(new TextNode($" Author:  {detail.AuthorName} <{detail.AuthorEmail}>").WithForeground(theme.TextDim).Height(1));
        lines.Add(new TextNode($" Date:    {detail.When:yyyy-MM-dd HH:mm:ss}").WithForeground(theme.TextDim).Height(1));

        if (detail.ParentShas.Count > 0)
            lines.Add(new TextNode($" Parents: {string.Join(", ", detail.ParentShas.Select(s => s[..Math.Min(7, s.Length)]))}").WithForeground(theme.TextDim).Height(1));

        if (detail.BranchLabels.Count > 0)
            lines.Add(new TextNode($" Refs:    {string.Join(", ", detail.BranchLabels)}").WithForeground(theme.Accent).Height(1));

        lines.Add(Layouts.Empty().Height(1));

        // Files section
        lines.Add(new TextNode($" Files ({detail.Files.Count})").WithForeground(theme.PanelTitle).Bold().Height(1));

        var expanded = ViewModel.ExpandedFiles.Value;
        var selectedIndex = ViewModel.SelectedFileIndex.Value;

        for (var i = 0; i < detail.Files.Count; i++)
        {
            var file = detail.Files[i];
            var isExpanded = expanded.Contains(i);
            var isSelected = i == selectedIndex;
            var arrow = isExpanded ? "▾" : "▸";
            var marker = isSelected ? "▸" : " ";
            var kindChar = file.Kind switch
            {
                ChangeKind.Added => "A",
                ChangeKind.Modified => "M",
                ChangeKind.Deleted => "D",
                ChangeKind.Renamed => "R",
                _ => "?"
            };

            var kindColor = file.Kind switch
            {
                ChangeKind.Added => theme.Success,
                ChangeKind.Deleted => theme.Error,
                _ => theme.Foreground,
            };

            var fileNode = new TextNode($" {marker}{arrow} {kindChar} {file.Path}")
                .WithForeground(isSelected ? theme.Selection : kindColor)
                .Height(1);
            lines.Add(fileNode);

            if (isExpanded)
            {
                foreach (var hunk in file.Hunks)
                {
                    lines.Add(new TextNode($"   @@ -{hunk.OldStart},{hunk.OldCount} +{hunk.NewStart},{hunk.NewCount} @@")
                        .WithForeground(theme.Accent).Height(1));

                    foreach (var line in hunk.Lines)
                    {
                        var (prefix, color) = line.Kind switch
                        {
                            DiffLineKind.Added => ("+", theme.Success),
                            DiffLineKind.Removed => ("-", theme.Error),
                            _ => (" ", theme.TextDim),
                        };

                        lines.Add(new TextNode($"   {prefix} {line.Content}").WithForeground(color).Height(1));
                    }
                }
            }
        }

        return Layouts.Vertical(lines.ToArray()).Fill();
    }

    public override void OnNavigatedTo()
    {
        base.OnNavigatedTo();

        // Esc goes back to graph
        KeyBindings.Register(ConsoleKey.Escape, () => Navigate("/git"));

        // Enter toggles file expand/collapse
        KeyBindings.Register(ConsoleKey.Enter, () =>
        {
            ViewModel.ToggleFile(ViewModel.SelectedFileIndex.Value);
            InvalidateLayout();
        });

        // Tab cycles to next file
        KeyBindings.Register(ConsoleKey.Tab, () =>
        {
            var detail = ViewModel.Detail.Value;
            if (detail is null || detail.Files.Count == 0) return;
            var next = (ViewModel.SelectedFileIndex.Value + 1) % detail.Files.Count;
            ViewModel.SelectedFileIndex.Value = next;
            InvalidateLayout();
        });

        // Shift+Tab cycles to previous file
        KeyBindings.Register(ConsoleKey.Tab, ConsoleModifiers.Shift, () =>
        {
            var detail = ViewModel.Detail.Value;
            if (detail is null || detail.Files.Count == 0) return;
            var prev = (ViewModel.SelectedFileIndex.Value - 1 + detail.Files.Count) % detail.Files.Count;
            ViewModel.SelectedFileIndex.Value = prev;
            InvalidateLayout();
        });

        // Subscribe to detail changes to rebuild layout
        ViewModel.Detail.Subscribe(_ => InvalidateLayout()).DisposeWith(Subscriptions);
        ViewModel.StatusMessage.Subscribe(_ => InvalidateLayout()).DisposeWith(Subscriptions);
    }
}
