namespace LTF.Domain.Racing;

/// <summary>
/// Which technical regulation era a carset races under. The engine supports more than one era
/// side by side and a carset selects the one it belongs to — the first concrete piece of
/// ADR-0010's regulation layer. <see cref="DrsEra"/> is the 2024/2025 rule set (a DRS /
/// slipstream overtaking aid, no energy budget); <see cref="ActiveAero2026"/> is the 2026 rule
/// set, where DRS is gone and overtaking uses a battery-limited Manual Override boost with
/// per-lap energy management (deplete the battery and the car de-rates).
/// </summary>
public enum RegulationEra
{
    /// <summary>2024/2025 rules: DRS / slipstream overtaking aid, no energy budget. The default.</summary>
    DrsEra,

    /// <summary>2026 rules: no DRS; a Manual Override electric boost drawn from a per-lap energy
    /// budget, and a depleted battery de-rates the car.</summary>
    ActiveAero2026,
}

/// <summary>
/// The active technical regulations for a race. Defaults to <see cref="RegulationEra.DrsEra"/>,
/// which reproduces the pre-2026 behaviour exactly; the 2026 fields below are read only when
/// <see cref="Era"/> is <see cref="RegulationEra.ActiveAero2026"/>. Authored per carset (M2's
/// optional <c>regulations</c> block) and, like <see cref="BalanceCoefficients"/>, tunable —
/// the defaults are a sensible neutral 2026 profile, not final tuning (calibrated in M10 / 26c).
/// </summary>
public sealed record RegulationSet
{
    /// <summary>The rule era this set describes. Default <see cref="RegulationEra.DrsEra"/>.</summary>
    public RegulationEra Era { get; init; } = RegulationEra.DrsEra;

    // ---- 2026: energy management (read only when Era is ActiveAero2026) ----

    /// <summary>Fraction of the energy budget harvested back each green lap (regeneration).</summary>
    public double EnergyRegenPerLap { get; init; } = 0.10;

    /// <summary>Energy drawn from the budget each time Manual Override is deployed to attack.</summary>
    public double ManualOverrideEnergyCost { get; init; } = 0.15;

    /// <summary>Overtake-chance boost from a Manual Override deployment — the 2026 stand-in for the
    /// DRS / slipstream boost, but paid for out of the energy budget.</summary>
    public double ManualOverrideBoost { get; init; } = 0.6;

    /// <summary>Energy level below which the battery can no longer sustain full power and the car
    /// de-rates (a lap-time penalty) until it recharges.</summary>
    public double DeRatingThreshold { get; init; } = 0.15;

    /// <summary>Lap-time penalty, in seconds, of a fully depleted battery, scaled by how far energy
    /// is below <see cref="DeRatingThreshold"/>.</summary>
    public double DeRatingPenaltySeconds { get; init; } = 1.5;

    /// <summary>The shared default: the DRS era with pre-2026 behaviour.</summary>
    public static RegulationSet Drs { get; } = new();

    /// <summary>A ready-made 2026 active-aero era with the field defaults.</summary>
    public static RegulationSet Aero2026 { get; } = new() { Era = RegulationEra.ActiveAero2026 };
}
