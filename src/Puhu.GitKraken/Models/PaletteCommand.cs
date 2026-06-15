namespace Puhu.GitKraken.Models;

public sealed record PaletteCommand(
    string Id,
    string Label,
    string[] Keywords,
    Func<Task> Execute);
