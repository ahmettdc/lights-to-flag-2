using System;
using Avalonia.Headless.XUnit;
using Avalonia.Platform;
using Xunit;

namespace LTF.App.Tests;

/// <summary>
/// The brand kit is embedded and addressable (M19m): the main window carries the app icon, and the logo
/// assets resolve from avares:// (the main menu wires them up in M20). Headless on all three CI platforms.
/// </summary>
public class BrandingTests
{
    [AvaloniaFact]
    public void Main_window_carries_the_app_icon()
    {
        var window = new global::LTF.App.MainWindow();

        Assert.NotNull(window.Icon);
    }

    [AvaloniaFact]
    public void Brand_logo_assets_resolve_from_avares()
    {
        Assert.True(AssetLoader.Exists(new Uri("avares://LTF.App/Assets/Branding/icon-512.png")));
        Assert.True(AssetLoader.Exists(new Uri("avares://LTF.App/Assets/Branding/logo-primary.png")));
    }
}
