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

    /// <summary>Points for leading at least one lap of the race (M9). 0 disables it.</summary>
    public int LeadingLapPoint { get; init; }

    /// <summary>Points for leading the most laps of the race (M9). 0 disables it.</summary>
    public int MostLapsLedPoint { get; init; }

    /// <summary>Points for finishing <paramref name="position"/> (1-based); 0 if out of the points.</summary>
    public int PointsFor(int position) =>
        position >= 1 && position <= RacePoints.Count ? RacePoints[position - 1] : 0;

    /// <summary>Sprint points for finishing <paramref name="position"/> (1-based); 0 if out (M9).</summary>
    public int SprintPointsFor(int position) =>
        position >= 1 && position <= SprintPoints.Count ? SprintPoints[position - 1] : 0;
}

/// <summary>
/// The series' economic rules (M13): what a team earns and the baseline costs it carries, all in
/// whole units of the carset's currency. Every field defaults to zero/empty, so a carset with no
/// <c>economy</c> block behaves exactly as before — no money moves.
/// </summary>
public sealed record EconomyRules
{
    /// <summary>Prize money by finishing position in the constructors' championship (1st, 2nd, …);
    /// empty disables prize money.</summary>
    public IReadOnlyList<long> PrizeMoney { get; init; } = [];

    /// <summary>Flat television income paid to every team each season.</summary>
    public long TvIncome { get; init; }

    /// <summary>Baseline operating cost incurred per race entered (freight, crew, consumables).</summary>
    public long OperatingCostPerRace { get; init; }

    /// <summary>Repair cost booked per crash/collision incident a team's cars are involved in.</summary>
    public long CrashCostPerIncident { get; init; }

    /// <summary>Prize money for finishing <paramref name="position"/> (1-based) in the constructors'
    /// championship; 0 if out of the paying positions.</summary>
    public long PrizeFor(int position) =>
        position >= 1 && position <= PrizeMoney.Count ? PrizeMoney[position - 1] : 0;
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

    /// <summary>Whether lapped cars are waved past to unlap themselves at a safety-car restart (M9).
    /// True (default) matches the built-in behaviour; false keeps lapped cars a lap down.</summary>
    public bool DriversUnlapUnderSafetyCar { get; init; } = true;

    /// <summary>The series' economic rules — prize money, TV income, baseline costs (M13). Empty by
    /// default, so a carset with no economy moves no money.</summary>
    public EconomyRules Economy { get; init; } = new();
}
