namespace LightsToFlag.Core.Simulation;

/// <summary>One competitor's position in the running order at the end of a lap.</summary>
public sealed record LapStanding
{
    public required string CompetitorId { get; init; }
    public required int Position { get; init; }

    /// <summary>Time behind the leader in seconds (0 for the leader).</summary>
    public required double GapToLeaderSeconds { get; init; }

    public required int LapsCompleted { get; init; }
    public required TyreCompound Tyre { get; init; }
    public required FinishStatus Status { get; init; }

    /// <summary>True if this competitor made a pit stop on this lap.</summary>
    public bool InPit { get; init; }
}

/// <summary>The full running order captured at the end of one lap (for live timing playback).</summary>
public sealed record LapSnapshot
{
    public required int Lap { get; init; }
    public required IReadOnlyList<LapStanding> Order { get; init; }

    public string LeaderId => Order.Count > 0 ? Order[0].CompetitorId : string.Empty;
}

/// <summary>A race result plus the lap-by-lap telemetry the UI plays back as live timing.</summary>
public sealed record RaceTelemetry
{
    public required RaceClassification Final { get; init; }
    public required IReadOnlyList<LapSnapshot> Laps { get; init; }
}
