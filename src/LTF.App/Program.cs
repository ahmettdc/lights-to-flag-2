using Avalonia;

namespace LTF.App;

/// <summary>
/// Desktop entry point. Kept deliberately thin — see ROADMAP.md Phase 3 for the
/// UI build-out. Any startup work must happen before the Avalonia app runs.
/// </summary>
internal static class Program
{
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    /// <summary>Avalonia configuration, shared by the runtime and design-time tooling.</summary>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
