namespace LightsToFlag.Core.Simulation;

/// <summary>How a competitor ended the race.</summary>
public enum FinishStatus
{
    Finished,
    Retired,
}

/// <summary>One competitor's race outcome.</summary>
public sealed record RaceEntryResult
{
    /// <summary>1-based finishing position (classification order).</summary>
    public required int Position { get; init; }

    public required string CompetitorId { get; init; }
    public required FinishStatus Status { get; init; }
    public required int LapsCompleted { get; init; }

    /// <summary>Total race time in seconds (running total, incl. pit losses).</summary>
    public required double TotalTimeSeconds { get; init; }

    /// <summary>Championship points earned (finishing + fastest-lap bonus).</summary>
    public required double Points { get; init; }

    public bool FastestLap { get; init; }
    public string? RetirementReason { get; init; }
}

/// <summary>The classified result of a race.</summary>
public sealed record RaceClassification
{
    public required IReadOnlyList<RaceEntryResult> Entries { get; init; }
    public required int TotalLaps { get; init; }
    public string? FastestLapCompetitorId { get; init; }

    public string Winner => Entries.Count > 0 ? Entries[0].CompetitorId : string.Empty;

    public RaceEntryResult? For(string competitorId) =>
        Entries.FirstOrDefault(e => e.CompetitorId == competitorId);
}
