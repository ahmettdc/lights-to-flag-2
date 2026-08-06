namespace LTF.App.Settings;

/// <summary>Simulation difficulty. Stored and shown, but deliberately NOT read by the engine in M20 — the
/// deterministic race and its golden digest must not move. A later engine milestone wires it.</summary>
public enum DifficultyLevel
{
    Easy,
    Normal,
    Hard,
    Hardcore,
}

/// <summary>Live-race playback speed (consumed by the race view in a later milestone).</summary>
public enum RaceSpeed
{
    Slow,
    Normal,
    Fast,
    VeryFast,
}

/// <summary>Interface text size (accessibility).</summary>
public enum TextSize
{
    Small,
    Normal,
    Large,
}

/// <summary>
/// Player settings (uieksikler item 9). Persisted by <see cref="SettingsStore"/>. Some settings are wired
/// now (autosave; interface scale, applied live at the root), others are stored and surfaced but consumed by
/// the milestone that owns them (race speed, audio, reduce-motion/high-contrast). Difficulty is stored but
/// inert w.r.t. the engine (see <see cref="DifficultyLevel"/>).
/// </summary>
public sealed record GameSettings
{
    public bool Autosave { get; init; } = true;
    public DifficultyLevel Difficulty { get; init; } = DifficultyLevel.Normal;
    public RaceSpeed RaceSpeed { get; init; } = RaceSpeed.Normal;

    /// <summary>Whole-interface zoom, 0.8–1.5 (accessibility). Applied live at the app root.</summary>
    public double InterfaceScale { get; init; } = 1.0;

    public TextSize TextSize { get; init; } = TextSize.Normal;
    public bool ReduceMotion { get; init; }
    public bool HighContrast { get; init; }

    public int MasterVolume { get; init; } = 80;
    public int MusicVolume { get; init; } = 70;
    public int SfxVolume { get; init; } = 80;

    /// <summary>The out-of-the-box defaults.</summary>
    public static GameSettings Default { get; } = new();
}
