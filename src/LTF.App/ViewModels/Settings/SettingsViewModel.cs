using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LTF.App.Mvvm;
using LTF.App.Settings;

namespace LTF.App.ViewModels.Settings;

/// <summary>
/// The settings screen (M20e). Edits a working copy seeded from the current <see cref="GameSettings"/>; Save
/// persists via the store and closes, Back discards. Reused from the main menu and (M20f) the in-shell
/// Settings row — the <c>onClose</c> callback returns to wherever it was opened.
/// </summary>
public sealed partial class SettingsViewModel : ViewModelBase
{
    private readonly ISettingsStore _store;
    private readonly Action _onClose;

    public SettingsViewModel(GameSettings current, ISettingsStore store, Action onClose)
    {
        _store = store;
        _onClose = onClose;

        _autosave = current.Autosave;
        _difficulty = current.Difficulty;
        _raceSpeed = current.RaceSpeed;
        _interfaceScale = current.InterfaceScale;
        _textSize = current.TextSize;
        _reduceMotion = current.ReduceMotion;
        _highContrast = current.HighContrast;
        _masterVolume = current.MasterVolume;
        _musicVolume = current.MusicVolume;
        _sfxVolume = current.SfxVolume;
    }

    [ObservableProperty] private bool _autosave;
    [ObservableProperty] private DifficultyLevel _difficulty;
    [ObservableProperty] private RaceSpeed _raceSpeed;
    [ObservableProperty] private double _interfaceScale;
    [ObservableProperty] private TextSize _textSize;
    [ObservableProperty] private bool _reduceMotion;
    [ObservableProperty] private bool _highContrast;
    [ObservableProperty] private int _masterVolume;
    [ObservableProperty] private int _musicVolume;
    [ObservableProperty] private int _sfxVolume;

    public DifficultyLevel[] DifficultyOptions { get; } = Enum.GetValues<DifficultyLevel>();
    public RaceSpeed[] RaceSpeedOptions { get; } = Enum.GetValues<RaceSpeed>();
    public TextSize[] TextSizeOptions { get; } = Enum.GetValues<TextSize>();

    /// <summary>The settings as currently edited (clamped to valid ranges).</summary>
    public GameSettings Current() => new()
    {
        Autosave = Autosave,
        Difficulty = Difficulty,
        RaceSpeed = RaceSpeed,
        InterfaceScale = Math.Clamp(InterfaceScale, 0.8, 1.5),
        TextSize = TextSize,
        ReduceMotion = ReduceMotion,
        HighContrast = HighContrast,
        MasterVolume = Math.Clamp(MasterVolume, 0, 100),
        MusicVolume = Math.Clamp(MusicVolume, 0, 100),
        SfxVolume = Math.Clamp(SfxVolume, 0, 100),
    };

    [RelayCommand]
    private void Save()
    {
        _store.Save(Current());
        _onClose();
    }

    [RelayCommand]
    private void Back() => _onClose();

    [RelayCommand]
    private void ResetToDefaults()
    {
        var d = GameSettings.Default;
        Autosave = d.Autosave;
        Difficulty = d.Difficulty;
        RaceSpeed = d.RaceSpeed;
        InterfaceScale = d.InterfaceScale;
        TextSize = d.TextSize;
        ReduceMotion = d.ReduceMotion;
        HighContrast = d.HighContrast;
        MasterVolume = d.MasterVolume;
        MusicVolume = d.MusicVolume;
        SfxVolume = d.SfxVolume;
    }
}
