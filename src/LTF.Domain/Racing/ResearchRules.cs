namespace LTF.Domain.Racing;

/// <summary>
/// The series' R&amp;D tuning (M14 / ADR-0016 + ADR-0020): how fast development progresses, how facilities
/// and staff scale it, how gains realise and when a breach of budget or validation fails. Like
/// <see cref="EconomyRules"/>, every field defaults to zero/neutral, so a carset with no research rules
/// does no development — the inert default.
/// </summary>
public sealed record ResearchRules
{
    /// <summary>Base development progress a project accrues per season before facility/staff scaling.
    /// 0 (the default) means no development happens at all.</summary>
    public int BaseProgressPerSeason { get; init; }

    /// <summary>Progress needed to advance a project one validation state.</summary>
    public int StepProgress { get; init; } = 100;

    /// <summary>How strongly facility levels scale progress (0 = facilities don't matter).</summary>
    public double FacilityWeight { get; init; }

    /// <summary>How strongly staff skill scales progress (0 = staff don't matter).</summary>
    public double StaffWeight { get; init; }

    /// <summary>Base design-to-track correlation percent, multiplied with a node's own correlation.</summary>
    public int CorrelationBaseline { get; init; } = 100;

    /// <summary>Development quota granted per level of the aero facilities (wind tunnel + CFD).</summary>
    public int QuotaPerFacilityLevel { get; init; }

    /// <summary>Base number of projects a team may run at once, before facility bonuses.</summary>
    public int BaseActiveProjects { get; init; } = 1;

    /// <summary>Fraction of the estimated minimum gain a realised gain must reach to be approved.</summary>
    public double ApproveThreshold { get; init; } = 1.0;

    /// <summary>Rework attempts a failed validation gets before the project is abandoned.</summary>
    public int MaxRetries { get; init; }

    /// <summary>Random spread on the realised gain (0 = deterministic to the estimate midpoint).</summary>
    public double RealizationSpread { get; init; }

    /// <summary>Regulation-readiness gained per season when a team invests in it.</summary>
    public int ReadinessGainPerSeason { get; init; }

    /// <summary>How much the quality-control facility shifts a car's reliability (M15 / ADR-0020). 0 (the
    /// default) means the facility has no reliability effect and the season is byte-identical. Above 0,
    /// each level of quality control above the neutral level 3 raises reliability (and below it lowers
    /// it) by this many points — the facilities' path to reliability, kept in the career layer.</summary>
    public double QualityControlReliabilityInfluence { get; init; }

    /// <summary>Cost multiplier for a Minor node (neutral 1.0).</summary>
    public double MinorCostMultiplier { get; init; } = 1.0;

    /// <summary>Cost multiplier for a Major node.</summary>
    public double MajorCostMultiplier { get; init; } = 1.0;

    /// <summary>Cost multiplier for an Ultimate node.</summary>
    public double UltimateCostMultiplier { get; init; } = 1.0;
}
