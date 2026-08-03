using System.Collections.ObjectModel;
using LTF.Domain.Common;

namespace LTF.Domain.Racing;

/// <summary>How championship points are awarded. Unlike the previous version, every
/// field here is meant to be consumed by the engine (see ROADMAP M9).</summary>
public sealed record PointsScheme
{
    /// <summary>Points for 1st, 2nd, … downwards.</summary>
    public required IReadOnlyList<int> RacePoints { get; init; }

    /// <summary>Points for the sprint race, if the series runs one.</summary>
    public IReadOnlyList<int> SprintPoints { get; init; } = [];

    public int PolePoint { get; init; }
    public int FastestLapPoint { get; init; }

    /// <summary>Points for finishing <paramref name="position"/> (1-based); 0 if out of the points.</summary>
    public int PointsFor(int position) =>
        position >= 1 && position <= RacePoints.Count ? RacePoints[position - 1] : 0;
}

/// <summary>
/// Series regulations for a carset. The engine reads all of it; a rule that exists here
/// has a corresponding effect in the simulation or season logic (acceptance criterion).
/// </summary>
public sealed record RulesSet
{
    public required string SeriesName { get; init; }
    public required PointsScheme Points { get; init; }

    public QualifyingFormat Qualifying { get; init; } = QualifyingFormat.Knockout;

    /// <summary>Age at which drivers are forced to retire during season rollover.</summary>
    public int RetirementAge { get; init; } = 40;

    public bool RefuellingAllowed { get; init; }
    public int MandatoryPitStops { get; init; }
    public bool BothDryCompoundsRequired { get; init; }

    /// <summary>How many of each life-limited component a car may use per season.</summary>
    public IReadOnlyDictionary<ComponentKind, int> ComponentAllocation { get; init; } =
        ReadOnlyDictionary<ComponentKind, int>.Empty;

    /// <summary>Grid places lost for each component used beyond the season allocation.</summary>
    public int GridPenaltyPerExtraComponent { get; init; } = 5;
}
