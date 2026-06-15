using Puhu.GitKraken.Models;
using Puhu.GitKraken.Services;
using Puhu.Plugin;
using R3;
using Termina.Layout;
using Termina.Reactive;

namespace Puhu.GitKraken.Pages;

public sealed class GitMainPage : ReactivePage<GitMainViewModel>, IKeyHintProvider
{
    private readonly ITabNavigator _tabNavigator;
    private readonly IRefreshController _refreshController;
    private readonly IThemeService _themeService;

    private SelectionListNode<RenderedGraphRow>? _graphList;
    private int _statusSelectedIndex;
    private bool _statusFocused;

    public GitMainPage(
        ITabNavigator tabNavigator,
        IRefreshController refreshController,
        IThemeService themeService)
    {
        _tabNavigator = tabNavigator;
        _refreshController = refreshController;
        _themeService = themeService;
    }

    public string[] GetKeyHints() =>
        ["Enter:Details", "R:Refresh", "S:Stage", "U:Unstage", "A:StageAll", "C:Commit", "Tab:SwitchPanel"];

    public override ILayoutNode BuildLayout()
    {
        var theme = _themeService.Current;

        var header = Layouts.Horizontal(
                new TextNode(" Branch: ")
                    .WithForeground(theme.TextDim),
                new TextNode(ViewModel.CurrentBranch.Value)
                    .WithForeground(theme.Accent),
                new TextNode($"  Commits: {ViewModel.CommitCount.Value}")
                    .WithForeground(theme.TextDim))
            .Height(1);

        if (ViewModel.StatusMessage.Value is { Length: > 0 } msg)
        {
            return Layouts.Vertical(
                header,
                new TextNode(msg)
                    .WithForeground(theme.Warning)
                    .AlignCenter()
                    .Fill());
        }

        // Graph panel (left)
        var rows = ViewModel.GraphRows.Value;
        _graphList = new SelectionListNode<RenderedGraphRow>(
            rows,
            row => $" {row.GraphPrefix}{row.Commit.Sha} {row.Commit.MessageShort}");
        _graphList
            .WithHighlightColors(theme.SelectionText, theme.Selection)
            .WithForeground(theme.Foreground)
            .WithFillHeight();

        var graphTitle = new TextNode(_statusFocused ? " Graph" : " Graph [active]")
            .WithForeground(_statusFocused ? theme.TextDim : theme.PanelTitle)
            .Bold()
            .Height(1);

        var graphPanel = Layouts.Vertical(graphTitle, _graphList);

        // Status panel (right)
        var statusContent = StatusPanelBuilder.Build(
            ViewModel.Status.Value,
            theme,
            _statusSelectedIndex,
            _statusFocused);

        var statusTitle = new TextNode(_statusFocused ? " Status [active]" : " Status")
            .WithForeground(_statusFocused ? theme.PanelTitle : theme.TextDim)
            .Bold()
            .Height(1);

        // Commit message bar
        var commitMsg = ViewModel.CommitMessage.Value;
        var commitPrompt = string.IsNullOrEmpty(commitMsg)
            ? " Commit message (C to edit)..."
            : $" > {commitMsg}";
        var commitBar = new TextNode(commitPrompt)
            .WithForeground(string.IsNullOrEmpty(commitMsg) ? theme.TextDim : theme.Foreground)
            .Height(1);

        var statusPanel = Layouts.Vertical(statusTitle, statusContent, commitBar);

        return Layouts.Vertical(
            header,
            Layouts.Horizontal(graphPanel, statusPanel));
    }

    public override void OnNavigatedTo()
    {
        base.OnNavigatedTo();

        KeyBindings.RegisterGlobalKeys(
            Shutdown,
            Navigate,
            _tabNavigator,
            _refreshController);

        KeyBindings.Register(ConsoleKey.R, () => ViewModel.Refresh());

        // Enter on graph: navigate to commit detail
        KeyBindings.Register(ConsoleKey.Enter, () =>
        {
            if (!_statusFocused)
            {
                var hash = _graphList?.HighlightedItem?.Value.Commit.FullSha;
                if (hash is not null)
                    Navigate($"/git/commit/{hash}");
            }
            else
            {
                // Stage/unstage the selected entry on Enter
                _ = HandleEnterOnStatusAsync();
            }
        });

        // Tab: switch focus between graph and status panels
        // Note: Tab is already taken by global tab switching, use F1/F2 or backtick instead
        KeyBindings.Register(ConsoleKey.F1, () =>
        {
            _statusFocused = false;
            InvalidateLayout();
        });

        KeyBindings.Register(ConsoleKey.F2, () =>
        {
            _statusFocused = true;
            InvalidateLayout();
        });

        // Navigation in status panel
        KeyBindings.Register(ConsoleKey.UpArrow, () =>
        {
            if (_statusFocused)
            {
                var total = StatusPanelBuilder.GetTotalEntries(ViewModel.Status.Value);
                if (total > 0)
                    _statusSelectedIndex = Math.Max(0, _statusSelectedIndex - 1);
                InvalidateLayout();
            }
        });

        KeyBindings.Register(ConsoleKey.DownArrow, () =>
        {
            if (_statusFocused)
            {
                var total = StatusPanelBuilder.GetTotalEntries(ViewModel.Status.Value);
                if (total > 0)
                    _statusSelectedIndex = Math.Min(total - 1, _statusSelectedIndex + 1);
                InvalidateLayout();
            }
        });

        // S: stage selected file
        KeyBindings.Register(ConsoleKey.S, () =>
        {
            if (!_statusFocused) return;
            var entry = StatusPanelBuilder.GetEntryAtIndex(ViewModel.Status.Value, _statusSelectedIndex);
            if (entry is not null)
                _ = ViewModel.StageFileAsync(entry.Path);
        });

        // U: unstage selected file
        KeyBindings.Register(ConsoleKey.U, () =>
        {
            if (!_statusFocused) return;
            var entry = StatusPanelBuilder.GetEntryAtIndex(ViewModel.Status.Value, _statusSelectedIndex);
            if (entry is not null)
                _ = ViewModel.UnstageFileAsync(entry.Path);
        });

        // A: stage all
        KeyBindings.Register(ConsoleKey.A, () =>
        {
            _ = ViewModel.StageAllAsync();
        });

        // C: commit (if there's a staged message already typed, commit it)
        KeyBindings.Register(ConsoleKey.C, () =>
        {
            if (!string.IsNullOrWhiteSpace(ViewModel.CommitMessage.Value))
                _ = CommitAndRefreshAsync();
        });

        // Subscribe to reactive property changes
        ViewModel.GraphRows.Subscribe(_ => InvalidateLayout()).DisposeWith(Subscriptions);
        ViewModel.StatusMessage.Subscribe(_ => InvalidateLayout()).DisposeWith(Subscriptions);
        ViewModel.Status.Subscribe(_ =>
        {
            _statusSelectedIndex = 0;
            InvalidateLayout();
        }).DisposeWith(Subscriptions);
        ViewModel.CommitMessage.Subscribe(_ => InvalidateLayout()).DisposeWith(Subscriptions);
    }

    private async Task HandleEnterOnStatusAsync()
    {
        var status = ViewModel.Status.Value;
        var entry = StatusPanelBuilder.GetEntryAtIndex(status, _statusSelectedIndex);
        if (entry is null) return;

        var (section, _) = StatusPanelBuilder.GetSectionForIndex(status, _statusSelectedIndex);
        if (section == StatusSection.Staged)
            await ViewModel.UnstageFileAsync(entry.Path);
        else
            await ViewModel.StageFileAsync(entry.Path);
    }

    private async Task CommitAndRefreshAsync()
    {
        var result = await ViewModel.CommitAsync();
        if (!result.Success && result.Error is not null)
        {
            ViewModel.StatusMessage.Value = $"Commit failed: {result.Error}";
            InvalidateLayout();
        }
    }
}
