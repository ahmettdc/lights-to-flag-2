using LTF.App.Settings;
using LTF.App.ViewModels.Settings;
using Xunit;

namespace LTF.App.Tests;

/// <summary>The settings view-model (M20e): Save persists edits and closes, Back discards, values clamp,
/// and Reset restores the defaults.</summary>
public class SettingsTests
{
    private sealed class InMemoryStore : ISettingsStore
    {
        public GameSettings Saved = GameSettings.Default;

        public GameSettings Load() => Saved;
        public void Save(GameSettings settings) => Saved = settings;
    }

    [Fact]
    public void Save_persists_edits_and_closes()
    {
        var store = new InMemoryStore();
        var closed = false;
        var vm = new SettingsViewModel(GameSettings.Default, store, onClose: () => closed = true);

        vm.Autosave = false;
        vm.Difficulty = DifficultyLevel.Hardcore;
        vm.InterfaceScale = 1.3;
        vm.SaveCommand.Execute(null);

        Assert.True(closed);
        Assert.False(store.Saved.Autosave);
        Assert.Equal(DifficultyLevel.Hardcore, store.Saved.Difficulty);
        Assert.Equal(1.3, store.Saved.InterfaceScale, 3);
    }

    [Fact]
    public void Back_closes_without_saving()
    {
        var store = new InMemoryStore();
        var closed = false;
        var vm = new SettingsViewModel(GameSettings.Default, store, onClose: () => closed = true);

        vm.Autosave = false;
        vm.BackCommand.Execute(null);

        Assert.True(closed);
        Assert.True(store.Saved.Autosave); // unchanged: Back discards
    }

    [Fact]
    public void Interface_scale_is_clamped_on_save()
    {
        var store = new InMemoryStore();
        var vm = new SettingsViewModel(GameSettings.Default, store, onClose: () => { });

        vm.InterfaceScale = 5.0; // out of range
        vm.SaveCommand.Execute(null);

        Assert.Equal(1.5, store.Saved.InterfaceScale, 3); // clamped to the maximum
    }

    [Fact]
    public void Reset_restores_the_defaults()
    {
        var vm = new SettingsViewModel(
            GameSettings.Default with { Autosave = false, Difficulty = DifficultyLevel.Hard },
            new InMemoryStore(),
            onClose: () => { });

        vm.ResetToDefaultsCommand.Execute(null);

        Assert.True(vm.Autosave);
        Assert.Equal(DifficultyLevel.Normal, vm.Difficulty);
    }
}
