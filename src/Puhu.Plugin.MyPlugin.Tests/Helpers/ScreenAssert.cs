using Termina.Terminal;

namespace Puhu.Plugin.MyPlugin.Tests.Helpers;

/// <summary>
/// Assertion helpers over a <see cref="VirtualTerminal"/>. <see cref="WaitForTextAsync"/> polls,
/// so it tolerates the app loop re-rendering on a background thread (a read mid-render simply
/// triggers another poll).
/// </summary>
public static class ScreenAssert
{
    public static void Contains(this VirtualTerminal terminal, string text)
    {
        var screen = terminal.ToString();
        Assert.True(screen.Contains(text), $"screen should contain \"{text}\".\nActual screen:\n{screen}");
    }

    public static void DoesNotContain(this VirtualTerminal terminal, string text)
    {
        var screen = terminal.ToString();
        Assert.False(screen.Contains(text), $"screen should not contain \"{text}\".\nActual screen:\n{screen}");
    }

    /// <summary>Poll until <paramref name="text"/> appears on screen, or fail after the timeout.</summary>
    public static async Task WaitForTextAsync(
        this VirtualTerminal terminal,
        string text,
        int timeoutMs = 5000,
        int pollIntervalMs = 50)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        while (Environment.TickCount64 < deadline)
        {
            if (terminal.ToString().Contains(text))
            {
                return;
            }
            await Task.Delay(pollIntervalMs);
        }

        var screen = terminal.ToString();
        Assert.True(screen.Contains(text),
            $"text \"{text}\" should have appeared within {timeoutMs}ms.\nFinal screen:\n{screen}");
    }
}
