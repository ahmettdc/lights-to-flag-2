namespace LTF.Domain.Racing;

/// <summary>
/// Global simulation tuning. This is the <em>shape</em> of a carset's balance data;
/// the values are authored per carset (loaded in M2) and swept for balance in M10.
/// Defaults are sensible neutral starting points, not final tuning.
/// </summary>
public sealed record BalanceCoefficients
{
    /// <summary>How much driver skill (vs. car) drives lap time. 0..1.</summary>
    public double DriverPaceWeight { get; init; } = 0.35;

    /// <summary>How much car performance drives lap time. 0..1.</summary>
    public double CarPaceWeight { get; init; } = 0.65;

    /// <summary>Spread of per-lap random variation, in seconds.</summary>
    public double RandomnessSpreadSeconds { get; init; } = 0.25;

    /// <summary>Baseline tyre wear per lap (fraction of life).</summary>
    public double TyreWearPerLap { get; init; } = 0.02;

    /// <summary>Baseline per-lap probability of a mechanical failure.</summary>
    public double ReliabilityFailureRate { get; init; } = 0.001;

    /// <summary>Baseline per-race probability of a safety car.</summary>
    public double SafetyCarBaseChance { get; init; } = 0.35;

    /// <summary>Baseline chance an overtake attempt succeeds when pace allows it.</summary>
    public double OvertakeBaseChance { get; init; } = 0.5;

    /// <summary>Lap-time penalty in the wet, as a fraction of dry pace.</summary>
    public double WetPaceLoss { get; init; } = 0.12;

    /// <summary>Lap-time cost of a full fuel load, in seconds, bled off as fuel burns.</summary>
    public double FuelLoadPenaltySeconds { get; init; } = 1.5;
}
