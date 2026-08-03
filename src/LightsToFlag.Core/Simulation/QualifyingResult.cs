namespace LightsToFlag.Core.Simulation;

/// <summary>One competitor's qualifying outcome.</summary>
public sealed record QualifyingEntry
{
    public required string CompetitorId { get; init; }

    /// <summary>1-based grid position.</summary>
    public required int GridPosition { get; init; }

    /// <summary>Best lap time in seconds, or null if the driver set no time.</summary>
    public double? BestLapSeconds { get; init; }
}

/// <summary>The full qualifying result, ordered by grid position.</summary>
public sealed record QualifyingResult
{
    public required IReadOnlyList<QualifyingEntry> Grid { get; init; }

    public string PolePosition => Grid.Count > 0 ? Grid[0].CompetitorId : string.Empty;

    /// <summary>The 1-based grid slot for a competitor, or 0 if not classified.</summary>
    public int PositionOf(string competitorId) =>
        Grid.FirstOrDefault(e => e.CompetitorId == competitorId)?.GridPosition ?? 0;
}
