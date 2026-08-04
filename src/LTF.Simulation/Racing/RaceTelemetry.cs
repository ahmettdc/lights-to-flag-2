namespace LTF.Simulation.Racing;

/// <summary>A competitor's position and gap at the end of a lap.</summary>
public readonly record struct LapStanding(
    string CompetitorId, int Position, double TotalTime, double GapToLeader);

/// <summary>The running order at the end of one lap, and the race-control state during it.</summary>
public sealed record LapSnapshot
{
    public required int Lap { get; init; }
    public required IReadOnlyList<LapStanding> Order { get; init; }

    /// <summary>Whether the lap ran green or under a neutralisation (M5d).</summary>
    public required NeutralizationState State { get; init; }
}

/// <summary>
/// Lap-by-lap record of a race — the raw material for live timing (M23), commentary and
/// statistics (M24). The event log (retirements, incidents, neutralisations) is added as
/// those systems land in M5b–M5e.
/// </summary>
public sealed record RaceTelemetry
{
    public required IReadOnlyList<LapSnapshot> Laps { get; init; }
}
