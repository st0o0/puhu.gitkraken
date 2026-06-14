using Puhu.Plugin.MyPlugin.Tests.Fixtures;
using Puhu.Plugin.MyPlugin.Tests.Helpers;

namespace Puhu.Plugin.MyPlugin.Tests;

/// <summary>
/// End-to-end tests for the plugin page: a real Termina app renders to a VirtualTerminal and
/// receives keys through a VirtualInputSource. Use these as a starting point — add a test per key
/// your page binds, asserting the resulting change with <c>Terminal.WaitForTextAsync(...)</c>.
/// </summary>
public sealed class MyPageE2ETests
{
    [Fact]
    public async Task Page_RendersGreeting()
    {
        await using var app = new PluginAppFixture();
        await app.StartAsync();

        await app.Terminal.WaitForTextAsync("Hello from MyPlugin!");
    }

    [Fact]
    public async Task Escape_QuitsApp()
    {
        await using var app = new PluginAppFixture();
        await app.StartAsync();
        await app.Terminal.WaitForTextAsync("Hello from MyPlugin!");

        app.SendKey(ConsoleKey.Escape);

        Assert.True(await app.WaitForShutdownAsync(), "Escape should shut the app down");
    }
}
