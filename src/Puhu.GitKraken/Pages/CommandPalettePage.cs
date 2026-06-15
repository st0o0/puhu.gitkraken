using Puhu.GitKraken.Models;
using Puhu.GitKraken.Services;
using Puhu.Plugin;
using R3;
using Termina.Layout;
using Termina.Reactive;

namespace Puhu.GitKraken.Pages;

public sealed class CommandPalettePage : ReactivePage<CommandPaletteViewModel>, IKeyHintProvider
{
    private readonly IThemeService _themeService;

    public CommandPalettePage(IThemeService themeService)
    {
        _themeService = themeService;
    }

    public string[] GetKeyHints() =>
        ["Esc:Back", "Enter:Run", "Up/Dn:Select"];

    public override ILayoutNode BuildLayout()
    {
        var theme = _themeService.Current;
        var results = ViewModel.Results.Value;
        var selectedIndex = ViewModel.SelectedIndex.Value;
        var query = ViewModel.Query.Value;

        var lines = new List<ILayoutNode>
        {
            new TextNode(" Command Palette")
                .WithForeground(theme.PanelTitle)
                .Bold()
                .Height(1),
            Layouts.Empty().Height(1),
            new TextNode($" > {query}_")
                .WithForeground(theme.Foreground)
                .Height(1),
            Layouts.Empty().Height(1),
        };

        if (results.Count == 0)
        {
            lines.Add(new TextNode(" No commands found.")
                .WithForeground(theme.TextDim)
                .Height(1));
        }
        else
        {
            for (var i = 0; i < results.Count; i++)
            {
                var cmd = results[i];
                var isSelected = i == selectedIndex;
                var fg = isSelected ? theme.SelectionText : theme.Foreground;
                var bg = isSelected ? theme.Selection : theme.Background;
                lines.Add(new TextNode($"  {cmd.Label}")
                    .WithForeground(fg)
                    .WithBackground(bg)
                    .Height(1));
            }
        }

        return Layouts.Vertical(lines.ToArray()).Fill();
    }

    public override void OnNavigatedTo()
    {
        base.OnNavigatedTo();

        KeyBindings.Register(ConsoleKey.Escape, () => Navigate("/git"));

        KeyBindings.Register(ConsoleKey.Enter, () =>
        {
            _ = RunSelectedAsync();
        });

        KeyBindings.Register(ConsoleKey.UpArrow, () =>
        {
            var count = ViewModel.Results.Value.Count;
            if (count > 0)
                ViewModel.SelectedIndex.Value = Math.Max(0, ViewModel.SelectedIndex.Value - 1);
            InvalidateLayout();
        });

        KeyBindings.Register(ConsoleKey.DownArrow, () =>
        {
            var count = ViewModel.Results.Value.Count;
            if (count > 0)
                ViewModel.SelectedIndex.Value = Math.Min(count - 1, ViewModel.SelectedIndex.Value + 1);
            InvalidateLayout();
        });

        ViewModel.Results.Subscribe(_ =>
        {
            ViewModel.SelectedIndex.Value = 0;
            InvalidateLayout();
        }).DisposeWith(Subscriptions);

        ViewModel.Query.Subscribe(_ => InvalidateLayout()).DisposeWith(Subscriptions);
    }

    private async Task RunSelectedAsync()
    {
        var results = ViewModel.Results.Value;
        var index = ViewModel.SelectedIndex.Value;
        if (index < 0 || index >= results.Count) return;

        var cmd = results[index];
        await cmd.Execute();
        Navigate("/git");
    }
}

public sealed class CommandPaletteViewModel : ReactiveViewModel
{
    private readonly CommandRegistry _registry;

    public ReactiveProperty<string> Query { get; } = new("");
    public ReactiveProperty<IReadOnlyList<PaletteCommand>> Results { get; } = new([]);
    public ReactiveProperty<int> SelectedIndex { get; } = new(0);

    public CommandPaletteViewModel(CommandRegistry registry)
    {
        _registry = registry;
    }

    public override void OnActivated()
    {
        base.OnActivated();
        Search("");
    }

    public void Search(string query)
    {
        Query.Value = query;
        Results.Value = string.IsNullOrWhiteSpace(query)
            ? _registry.GetAll()
            : _registry.Search(query);
        SelectedIndex.Value = 0;
        RequestRedraw();
    }

    public override void Dispose()
    {
        Query.Dispose();
        Results.Dispose();
        SelectedIndex.Dispose();
        base.Dispose();
    }
}
