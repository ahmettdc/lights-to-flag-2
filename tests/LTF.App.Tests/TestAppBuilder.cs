using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(LTF.App.Tests.TestAppBuilder))]

namespace LTF.App.Tests;

/// <summary>
/// Headless Avalonia entry point for the UI tests. Mirrors <see cref="LTF.App.Program.BuildAvaloniaApp"/>
/// but swaps the desktop windowing backend (<c>UsePlatformDetect</c>) for the headless platform, so
/// <c>[AvaloniaFact]</c> tests can construct and drive the real <see cref="LTF.App.App"/> — its theme,
/// resources and controls — on Linux CI and the Windows/macOS matrix without a display. This is the
/// whole reason the UI is Avalonia rather than WPF (ADR-0002).
///
/// Uses Skia (<c>UseHeadlessDrawing = false</c>) rather than the headless stub renderer: the stub font
/// manager cannot create glyph typefaces for the embedded brand fonts (Saira Condensed / Chakra Petch /
/// Archivo), so any text laid out in them throws. Skia loads the avares:// fonts and shapes text
/// offscreen — still no display required — which the shell needs on every screen.
/// </summary>
public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<global::LTF.App.App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .WithInterFont();
}
