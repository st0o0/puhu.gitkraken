using Puhu.Plugin.MyPlugin.Pages;

namespace Puhu.Plugin.MyPlugin.Tests;

public class MyPluginTests
{
    [Fact]
    public void Plugin_exposes_its_name()
    {
        var plugin = new MyPlugin();

        Assert.Equal("MyPlugin", plugin.Name);
    }

    [Fact]
    public void ViewModel_starts_with_the_greeting()
    {
        using var viewModel = new MyViewModel();

        Assert.Equal("Hello from MyPlugin!", viewModel.StatusText.Value);
    }
}
