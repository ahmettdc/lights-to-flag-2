using Xunit;

namespace LTF.App.Tests;

/// <summary>
/// Proves the Avalonia app assembly builds and links on Linux CI — the whole
/// reason the UI is Avalonia rather than WPF (see ADR-0002). These are
/// reference/type checks only; full headless rendering tests arrive in M19
/// when there is real UI to drive.
/// </summary>
public class SmokeTests
{
    [Fact]
    public void App_type_is_present()
    {
        Assert.NotNull(typeof(global::LTF.App.App));
        Assert.True(typeof(Avalonia.Application).IsAssignableFrom(typeof(global::LTF.App.App)));
    }

    [Fact]
    public void MainWindow_type_is_present()
    {
        Assert.NotNull(typeof(global::LTF.App.MainWindow));
        Assert.True(typeof(Avalonia.Controls.Window).IsAssignableFrom(typeof(global::LTF.App.MainWindow)));
    }
}
