namespace LightsToFlag.Core.Career;

/// <summary>A compact, serializable record of one competitor's result in a round.</summary>
public sealed record RoundEntry
{
    public string CompetitorId { get; init; } = "";
    public int Position { get; init; }
    public double Points { get; init; }
    public bool Finished { get; init; }
    public bool FastestLap { get; init; }
}

/// <summary>The result of a single completed round.</summary>
public sealed record RoundResult
{
    public int RoundIndex { get; init; }
    public string CircuitName { get; init; } = "";
    public string PolePositionId { get; init; } = "";
    public IReadOnlyList<RoundEntry> Entries { get; init; } = Array.Empty<RoundEntry>();

    public string WinnerId => Entries.Count > 0 ? Entries[0].CompetitorId : "";
}
