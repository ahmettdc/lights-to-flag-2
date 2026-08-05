using LTF.Domain.Common;

namespace LTF.Simulation.Practice;

/// <summary>
/// The benefit a car carries out of practice into the rest of the weekend: a one-lap gain for
/// qualifying, a per-lap gain for the race, and a multiplier on the driver's error chance.
/// The neutral <see cref="None"/> value (no gain, error factor 1) is what a session with no
/// practice uses, and it leaves qualifying and the race bit-for-bit unchanged.
/// </summary>
public sealed record PracticeSetup
{
    /// <summary>Lap-time gain applied to each qualifying lap, in seconds (≥ 0).</summary>
    public double QualifyingBonusSeconds { get; init; }

    /// <summary>Lap-time gain applied to each green race lap, in seconds (≥ 0).</summary>
    public double RaceBonusSeconds { get; init; }

    /// <summary>Multiplier on the driver's per-lap error chance in the race (≤ 1 fewer mistakes).</summary>
    public double ErrorFactor { get; init; } = 1.0;

    /// <summary>No practice benefit — the neutral default.</summary>
    public static PracticeSetup None { get; } = new();
}

/// <summary>One car's practice outcome: the program it ran, how much usable data the session
/// produced (0..1), and the resulting <see cref="PracticeSetup"/>.</summary>
public sealed record PracticeEntry
{
    public required string CompetitorId { get; init; }
    public required PracticeProgram Program { get; init; }
    public required double DataQuality { get; init; }
    public required PracticeSetup Setup { get; init; }
}

/// <summary>
/// The outcome of a practice session across the field. <see cref="Setups"/> is the per-car
/// benefit map fed into <c>QualifyingSimulator.Run</c> and <c>RaceSimulator.Run</c>.
/// </summary>
public sealed record PracticeResult
{
    public required IReadOnlyList<PracticeEntry> Entries { get; init; }
    public required IReadOnlyDictionary<string, PracticeSetup> Setups { get; init; }
}
