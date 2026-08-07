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

    /// <summary>Financial penalty for a cost-cap breach, as a percentage of the overspend (M13 / ADR-0010):
    /// e.g. 100 fines the full overspend, 400 fines four times it, 0 disables the fine. Only bites when a
    /// team actually has a cost cap.</summary>
    public int CostCapFinePercent { get; init; }

    /// <summary>Constructor points deducted per unit of cost-cap overspend (M13 / ADR-0010): the deduction
    /// is <c>overspend / CostCapPointsPerOverage</c>. 0 disables the points penalty.</summary>
    public long CostCapPointsPerOverage { get; init; }

    /// <summary>Prize money for finishing <paramref name="position"/> (1-based) in the constructors'
    /// championship; 0 if out of the paying positions.</summary>
    public long PrizeFor(int position) =>
        position >= 1 && position <= PrizeMoney.Count ? PrizeMoney[position - 1] : 0;
}

/// <summary>
/// The series' banking rules (ADR-0029): the interest the bank quotes, how far a team may borrow, and the
/// enforcement terms on default. Every field defaults to zero/off, so a carset with no <c>bank</c> block
/// offers no credit and moves no money — the inert default (<see cref="IsActive"/> false). Amounts derived
/// from these are whole units of the carset's currency; percentages are whole ints (no floats, deterministic).
/// </summary>
public sealed record BankRules
{
    /// <summary>Base annual interest rate (percent) the best-credit team is quoted, before any risk premium.</summary>
    public int BaseRatePercent { get; init; }

    /// <summary>Extra interest (percent) a worst-credit team pays on top of the base rate; the premium scales
    /// from this (at credit 0) down to 0 (at credit 100).</summary>
    public int MaxRiskPremiumPercent { get; init; }

    /// <summary>Borrowing capacity as a percent of a team's serviceable annual revenue (prize + TV + sponsor),
    /// before the credit-score multiplier. 0 (default) disables borrowing.</summary>
    public int MaxLoanToRevenuePercent { get; init; }

    /// <summary>Late-payment penalty added to the outstanding balance on a missed instalment, as a percent of
    /// the missed payment.</summary>
    public int LatePenaltyPercent { get; init; }

    /// <summary>Missed payments (on a single loan) before the bank forces an asset sale — icra step 2.</summary>
    public int AssetSeizureAfterMisses { get; init; } = 2;

    /// <summary>Missed payments (on a single loan) before the terminal administrative penalty — icra step 3.
    /// The career always continues (no firing); the penalty is a constructor points deduction + heavy
    /// liquidation.</summary>
    public int InsolvencyAfterMisses { get; init; } = 4;

    /// <summary>Constructor points docked at the terminal insolvency step.</summary>
    public int InsolvencyPointsPenalty { get; init; }

    /// <summary>Whether the bank offers credit at all (any borrowing capacity configured).</summary>
    public bool IsActive => MaxLoanToRevenuePercent > 0;
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

    /// <summary>Base life, in rounds, of each component before it must be replaced (M15). Empty (the
    /// default) derives an even split from the season length and the allocation, so every component
    /// exactly fits its quota and no grid penalty is ever incurred.</summary>
    public IReadOnlyDictionary<ComponentKind, int> ComponentLifeRounds { get; init; } =
        ReadOnlyDictionary<ComponentKind, int>.Empty;

    /// <summary>How much a car's low reliability shortens its component life (M15). 0 (the default)
    /// means reliability has no effect — every car wears components at the same rate and, with the
    /// default life, exactly fits its allocation, so the grid is never reordered. Above 0, a less
    /// reliable car wears its components faster, overruns its allocation and takes grid penalties.</summary>
    public double ComponentReliabilityWearInfluence { get; init; }

    /// <summary>Whether lapped cars are waved past to unlap themselves at a safety-car restart (M9).
    /// True (default) matches the built-in behaviour; false keeps lapped cars a lap down.</summary>
    public bool DriversUnlapUnderSafetyCar { get; init; } = true;

    /// <summary>The series' economic rules — prize money, TV income, baseline costs (M13). Empty by
    /// default, so a carset with no economy moves no money.</summary>
    public EconomyRules Economy { get; init; } = new();

    /// <summary>The series' banking rules — loans, interest and credit (ADR-0029). Inert by default
    /// (<see cref="BankRules.IsActive"/> false), so a carset with no bank block offers no credit.</summary>
    public BankRules Bank { get; init; } = new();

    /// <summary>The series' R&amp;D tuning — development speed, facility/staff scaling, validation (M14).
    /// Neutral by default, so a carset with no research rules does no development.</summary>
    public ResearchRules Research { get; init; } = new();

    /// <summary>The series' driver-development tuning — aging, growth and decline (M18 / ADR-0015). Neutral
    /// by default (<see cref="DriverDevelopmentRules.IsActive"/> false), so a carset with no development
    /// block ages and develops no one.</summary>
    public DriverDevelopmentRules DriverDevelopment { get; init; } = new();

    /// <summary>How hard an unprepared team is set back when a regulation change lands (M18 / ADR-0010): the
    /// drop to the car rating the change favours is <c>(100 − readiness) / 100 × magnitude × this</c>. 0
    /// (the default) means a regulation change costs the field nothing — the inert default.</summary>
    public double RegulationUnreadinessPenalty { get; init; }
}
