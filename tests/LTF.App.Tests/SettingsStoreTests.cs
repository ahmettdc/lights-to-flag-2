using System.IO;
using LTF.App.Settings;
using Xunit;

namespace LTF.App.Tests;

/// <summary>The settings store (M20e): defaults when absent, a save→load round-trip, over a temp file.</summary>
public class SettingsStoreTests
{
    private static SettingsStore Fresh() =>
        new(Path.Combine(Directory.CreateTempSubdirectory().FullName, "settings.json"));

    [Fact]
    public void Missing_file_yields_defaults() =>
        Assert.Equal(GameSettings.Default, Fresh().Load());

    [Fact]
    public void Save_then_load_round_trips()
    {
        var store = Fresh();
        var settings = GameSettings.Default with
        {
            Autosave = false,
            Difficulty = DifficultyLevel.Hard,
            InterfaceScale = 1.25,
            MasterVolume = 40,
        };

        store.Save(settings);

        Assert.Equal(settings, store.Load());
    }
}
