using System;
using System.IO;

namespace LTF.App.Session;

/// <summary>
/// Where the app keeps player data on disk: saves and settings under the platform's application-data folder
/// (e.g. <c>%AppData%/LightsToFlag2</c> on Windows, <c>~/.config/LightsToFlag2</c> on Linux). Centralised so
/// every store agrees on one location, mirroring how <see cref="SessionLoader"/>/<see cref="CarsetCatalog"/>
/// keep bundled content next to the app.
/// </summary>
public static class AppPaths
{
    /// <summary>The app's writable data root.</summary>
    public static string Root =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LightsToFlag2");

    /// <summary>The saves folder under <see cref="Root"/>.</summary>
    public static string SavesDir => Path.Combine(Root, "saves");

    /// <summary>The settings file under <see cref="Root"/> (M20e).</summary>
    public static string SettingsFile => Path.Combine(Root, "settings.json");
}
