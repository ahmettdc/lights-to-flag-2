namespace LTF.Simulation.Racing;

/// <summary>One competitor's line in the final result.</summary>
public sealed record RaceClassificationEntry
{
    public required int Position { get; init; }
    public required string CompetitorId { get; init; }
    public required FinishStatus Status { get; init; }
    public string? RetirementReason { get; init; }

    public required int Laps { get; init; }
    public required double TotalTime { get; init; }
    public required double GapToLeader { get; init; }
    public required double BestLap { get; init; }

    /// <summary>Representative top speed reached (kph), from power unit and circuit character.</summary>
    public required double TopSpeed { get; init; }

    public required int Points { get; init; }

    /// <summary>The competitor's class (M9); empty for a single-class field.</summary>
    public string ClassId { get; init; } = "";

    /// <summary>Position within the class (M9); equal to <see cref="Position"/> for a single class.</summary>
    public int ClassPosition { get; init; }
}

/// <summary>The outcome of a race: the classification, the event log, the fastest lap and telemetry.</summary>
public sealed record RaceResult
{
    public required IReadOnlyList<RaceClassificationEntry> Classification { get; init; }
    public required RaceTelemetry Telemetry { get; init; }

    /// <summary>Everything notable that happened, in the order it happened (M5b: retirements).</summary>
    public required IReadOnlyList<RaceEvent> Events { get; init; }

    public string? FastestLapCompetitorId { get; init; }
    public double FastestLapTime { get; init; }

    /// <summary>The winner's competitor id, or null if nobody was classified.</summary>
    public string? WinnerId =>
        Classification.Count > 0 ? Classification[0].CompetitorId : null;
}
