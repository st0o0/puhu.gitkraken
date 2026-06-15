using Puhu.GitKraken.Services;
using Puhu.Plugin;
using R3;
using Termina.Layout;
using Termina.Reactive;

namespace Puhu.GitKraken.Pages;

public sealed class GraphPage : ReactivePage<GraphViewModel>, IKeyHintProvider
{
    private readonly ITabNavigator _tabNavigator;
    private readonly IRefreshController _refreshController;
    private readonly IThemeService _themeService;
    private SelectionListNode<RenderedGraphRow>? _list;

    public GraphPage(
        ITabNavigator tabNavigator,
        IRefreshController refreshController,
        IThemeService themeService)
    {
        _tabNavigator = tabNavigator;
        _refreshController = refreshController;
        _themeService = themeService;
    }

    public string[] GetKeyHints() =>
        ["Enter:Details", "R:Refresh"];

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

        var rows = ViewModel.Rows.Value;
        _list = new SelectionListNode<RenderedGraphRow>(
            rows,
            row => $" {row.GraphPrefix}{row.Commit.Sha} {row.Commit.MessageShort}");
        _list
            .WithHighlightColors(theme.SelectionText, theme.Selection)
            .WithForeground(theme.Foreground)
            .WithFillHeight();

        return Layouts.Vertical(header, _list);
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

        KeyBindings.Register(ConsoleKey.Enter, () =>
        {
            var hash = _list?.HighlightedItem?.Value.Commit.FullSha;
            if (hash is not null)
            {
                Navigate($"/git/commit/{hash}");
            }
        });

        ViewModel.Rows.Subscribe(_ => InvalidateLayout()).DisposeWith(Subscriptions);
        ViewModel.StatusMessage.Subscribe(_ => InvalidateLayout()).DisposeWith(Subscriptions);
    }
}