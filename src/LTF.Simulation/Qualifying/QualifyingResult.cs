using LTF.Domain.Common;
using LTF.Simulation.Laps;

namespace LTF.Simulation.Qualifying;

/// <summary>
/// One driver's place on the qualifying grid: where they start, their grid-deciding lap and
/// how deep into a knockout they got. <see cref="Part"/> is the deepest part they took
/// part in (higher is further; 1 for the single-lap and single-session formats), and
/// <see cref="BestLap"/> is the best lap they set in that part — the time their slot is
/// decided by.
/// </summary>
public sealed record QualifyingEntry
{
    public required int GridPosition { get; init; }
    public required string CompetitorId { get; init; }
    public required int Part { get; init; }
    public required double BestLap { get; init; }
    public SectorTimes BestSectors { get; init; }
}

/// <summary>
/// The outcome of a qualifying session: the starting grid in order, the format that produced
/// it and the pole time. The career layer (M11) turns <see cref="StartingOrder"/> into the
/// race's entry list; grid penalties (M7 / ADR-0010) are applied on top of this order.
/// </summary>
public sealed record QualifyingResult
{
    /// <summary>The grid in starting order (position 1 first).</summary>
    public required IReadOnlyList<QualifyingEntry> Grid { get; init; }

    public required QualifyingFormat Format { get; init; }

    public string? PoleCompetitorId { get; init; }
    public double PoleTime { get; init; }

    /// <summary>Competitor ids in starting order — the entry-list order for the race.</summary>
    public IReadOnlyList<string> StartingOrder => Grid.Select(e => e.CompetitorId).ToList();
}
