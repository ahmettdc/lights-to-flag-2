using System.Collections.ObjectModel;

namespace LTF.Simulation.Racing;

/// <summary>How the field is released at the start of a race.</summary>
public enum RaceStartType
{
    /// <summary>A standing start from the grid — the default.</summary>
    Standing,

    /// <summary>A rolling start behind a pace car — gentler getaways, no jump starts.</summary>
    Rolling,
}

/// <summary>
/// The per-race format for a single <see cref="RaceSimulator.Run"/>: which points table applies,
/// how the field starts, how long the race is, who is on pole (for the pole point) and any success
/// ballast. The <see cref="Standard"/> default reproduces a standard feature race, so passing it —
/// or <c>null</c> — leaves the race bit-for-bit unchanged. Series-constant rules stay on
/// <c>RulesSet</c> and point values on <c>PointsScheme</c>; this bundle carries only the per-race
/// selectors (M9). Each field is wired in its own M9 sub-step; until then it sits at its neutral
/// default.
/// </summary>
public sealed record RaceFormat
{
    /// <summary>Score the finish from the sprint points table rather than the race table (M9f).</summary>
    public bool IsSprint { get; init; }

    /// <summary>Standing (default) or rolling start (M9e).</summary>
    public RaceStartType StartType { get; init; } = RaceStartType.Standing;

    /// <summary>Target race duration in seconds; 0 (default) uses the circuit's lap count (M9a).</summary>
    public double TimedDurationSeconds { get; init; }

    /// <summary>Fixed lap-count override; 0 (default) falls back to the timed duration or the
    /// circuit's own lap count (M9a). Takes precedence over <see cref="TimedDurationSeconds"/>.</summary>
    public int LapOverride { get; init; }

    /// <summary>The pole-sitter's competitor id, for the pole point; <c>null</c> (default) awards
    /// none (M9b).</summary>
    public string? PoleSitterId { get; init; }

    /// <summary>Per-car success ballast in seconds of lap time; empty (default) applies none (M9c).</summary>
    public IReadOnlyDictionary<string, double> Ballast { get; init; } =
        ReadOnlyDictionary<string, double>.Empty;

    /// <summary>A standard feature race — the neutral default that leaves a race unchanged.</summary>
    public static RaceFormat Standard { get; } = new();
}
