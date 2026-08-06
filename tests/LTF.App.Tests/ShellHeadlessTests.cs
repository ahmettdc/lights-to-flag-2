using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Xunit;

namespace LTF.App.Tests;

/// <summary>
/// First headless render test — proves the <c>Avalonia.Headless.XUnit</c> pipeline can build the real
/// <see cref="LTF.App.App"/> and show a window on all three CI platforms. It runs against the current
/// M0 skeleton window on purpose (M19a de-risk): any failure here is the harness, not new shell UI.
/// Later M19 sub-steps replace this window and assert the real navigation shell.
/// </summary>
public class ShellHeadlessTests
{
    [AvaloniaFact]
    public void Main_window_shows_headless()
    {
        var window = new global::LTF.App.MainWindow();

        window.Show();
        Dispatcher.UIThread.RunJobs();

        Assert.True(window.IsVisible);
    }
}
