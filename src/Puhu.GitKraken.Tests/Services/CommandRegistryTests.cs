using Puhu.GitKraken.Models;
using Puhu.GitKraken.Services;

namespace Puhu.GitKraken.Tests.Services;

public sealed class CommandRegistryTests
{
    [Fact]
    public void Register_and_retrieve_all_commands()
    {
        var registry = new CommandRegistry();
        registry.Register(new PaletteCommand("test.a", "Alpha Command", ["alpha", "first"], () => Task.CompletedTask));
        registry.Register(new PaletteCommand("test.b", "Beta Command", ["beta", "second"], () => Task.CompletedTask));

        var all = registry.GetAll();
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public void Search_filters_by_label()
    {
        var registry = new CommandRegistry();
        registry.Register(new PaletteCommand("a", "Create Branch", ["branch", "new"], () => Task.CompletedTask));
        registry.Register(new PaletteCommand("b", "Delete Branch", ["branch", "remove"], () => Task.CompletedTask));
        registry.Register(new PaletteCommand("c", "Push", ["remote", "push"], () => Task.CompletedTask));

        var results = registry.Search("branch");
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Contains("Branch", r.Label));
    }

    [Fact]
    public void Search_matches_keywords()
    {
        var registry = new CommandRegistry();
        registry.Register(new PaletteCommand("a", "Create Branch", ["branch", "new"], () => Task.CompletedTask));
        registry.Register(new PaletteCommand("b", "Push", ["remote", "upload"], () => Task.CompletedTask));

        var results = registry.Search("remote");
        Assert.Single(results);
        Assert.Equal("Push", results[0].Label);
    }

    [Fact]
    public void Search_is_case_insensitive()
    {
        var registry = new CommandRegistry();
        registry.Register(new PaletteCommand("a", "Merge Branch", ["merge"], () => Task.CompletedTask));

        var results = registry.Search("MERGE");
        Assert.Single(results);
    }

    [Fact]
    public void Search_empty_query_returns_all()
    {
        var registry = new CommandRegistry();
        registry.Register(new PaletteCommand("a", "Alpha", [], () => Task.CompletedTask));
        registry.Register(new PaletteCommand("b", "Beta", [], () => Task.CompletedTask));

        var results = registry.Search("");
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void Search_no_match_returns_empty()
    {
        var registry = new CommandRegistry();
        registry.Register(new PaletteCommand("a", "Push", ["remote"], () => Task.CompletedTask));

        var results = registry.Search("zzzzz");
        Assert.Empty(results);
    }
}
