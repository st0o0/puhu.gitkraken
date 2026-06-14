using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Puhu.Plugin;
using Puhu.Plugin.MyPlugin.Pages;
using Termina.Hosting;
using Termina.Input;
using Termina.Terminal;

namespace Puhu.Plugin.MyPlugin.Tests.Fixtures;

/// <summary>
/// Hosts the plugin's page in a real Termina application driven by an in-memory
/// <see cref="VirtualTerminal"/> and <see cref="VirtualInputSource"/>, so tests can press keys
/// and assert on the rendered screen — the same input → render pipeline used at runtime.
///
/// Register the virtual terminal BEFORE <c>AddTermina</c> (it uses TryAddSingleton, so it skips
/// its own real terminal if one is already registered) and feed keys through the virtual input.
/// </summary>
public sealed class PluginAppFixture : IAsyncDisposable
{
    public VirtualTerminal Terminal { get; }
    public VirtualInputSource Input { get; }

    private IHost? _host;

    public PluginAppFixture(int width = 80, int height = 24)
    {
        Terminal = new VirtualTerminal(width, height);
        Input = new VirtualInputSource();
    }

    public async Task StartAsync(string startRoute = "/myplugin")
    {
        var builder = Host.CreateApplicationBuilder();
        var services = builder.Services;

        services.AddSingleton<IAnsiTerminal>(Terminal);

        // Your page's constructor dependencies go here. MyPage only needs an ITabNavigator;
        // add AddSingleton<...> registrations for any services your real plugin pages require.
        services.AddSingleton<ITabNavigator>(new NoTabsNavigator());

        services.AddTerminaVirtualInput(Input);
        services.AddTermina(startRoute, termina =>
            termina.RegisterRoute<MyPage, MyViewModel>("/myplugin"));

        _host = builder.Build();
        await _host.StartAsync();
    }

    /// <summary>Enqueue a key press; the app loop renders after processing it.</summary>
    public void SendKey(ConsoleKey key, bool shift = false, bool alt = false, bool control = false)
        => Input.EnqueueKey(key, shift, alt, control);

    /// <summary>Resolves once the app has begun shutting down (e.g. a quit key fired); false on timeout.</summary>
    public async Task<bool> WaitForShutdownAsync(int timeoutMs = 5000)
    {
        var lifetime = _host!.Services.GetRequiredService<IHostApplicationLifetime>();
        var tcs = new TaskCompletionSource();
        await using var reg = lifetime.ApplicationStopping.Register(() => tcs.TrySetResult());
        if (lifetime.ApplicationStopping.IsCancellationRequested)
        {
            return true;
        }
        return await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs)) == tcs.Task;
    }

    public async ValueTask DisposeAsync()
    {
        Input.Complete();
        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
        }
    }

    private sealed class NoTabsNavigator : ITabNavigator
    {
        public bool HasTabs => false;
        public void CycleTab(Action<string> navigate, int delta) { }
    }
}
