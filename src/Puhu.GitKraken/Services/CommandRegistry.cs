using Puhu.GitKraken.Models;

namespace Puhu.GitKraken.Services;

public sealed class CommandRegistry
{
    private readonly List<PaletteCommand> _commands = [];

    public void Register(PaletteCommand command) => _commands.Add(command);

    public IReadOnlyList<PaletteCommand> GetAll() => _commands;

    public IReadOnlyList<PaletteCommand> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return _commands;

        var lowerQuery = query.ToLowerInvariant();
        var tokens = lowerQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return _commands
            .Where(c => tokens.All(token =>
                c.Label.Contains(token, StringComparison.OrdinalIgnoreCase) ||
                c.Id.Contains(token, StringComparison.OrdinalIgnoreCase) ||
                c.Keywords.Any(k => k.Contains(token, StringComparison.OrdinalIgnoreCase))))
            .ToList();
    }
}
