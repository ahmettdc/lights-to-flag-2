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

    /// <summary>Baseline component health lost per lap (fraction of full life). Reliable
    /// cars wear slower; a harder engine mode wears faster.</summary>
    public double ComponentHealthLossPerLap { get; init; } = 0.006;

    /// <summary>Lap-time cost, in seconds, of nursing a badly worn car home (limp mode),
    /// scaled by how far the weakest component is below the limp threshold.</summary>
    public double LimpPaceLossSeconds { get; init; } = 2.5;

    /// <summary>Lap-time gain, in seconds, from running the engine harder (push vs. standard);
    /// conserve gives the same amount back.</summary>
    public double EngineModePaceGainSeconds { get; init; } = 0.12;

    /// <summary>How much a harder engine mode multiplies failure risk and wear (push);
    /// conserve applies its inverse.</summary>
    public double EngineModeRiskFactor { get; init; } = 1.8;

    /// <summary>Baseline per-lap probability of a driver error, before skill scaling.</summary>
    public double DriverErrorBaseRate { get; init; } = 0.01;

    /// <summary>Baseline per-lap probability of contact with a nearby car, before skill scaling.</summary>
    public double CollisionBaseRate { get; init; } = 0.006;

    /// <summary>Probability of a botched start (jump start / bog-down) per car.</summary>
    public double StartIncidentRate { get; init; } = 0.015;

    /// <summary>How much more likely incidents are on lap 1 (first-corner chaos).</summary>
    public double FirstLapIncidentMultiplier { get; init; } = 4.0;

    /// <summary>Lap-time cost of a driver error (lock-up / off-track), scaled by severity.</summary>
    public double DriverErrorTimeLossSeconds { get; init; } = 2.0;

    /// <summary>Lap-time cost of contact, scaled by severity.</summary>
    public double CollisionTimeLossSeconds { get; init; } = 6.0;

    /// <summary>Time lost off an ideal getaway, scaled by how poor the driver's start skill is.</summary>
    public double StartSkillSeconds { get; init; } = 0.6;

    /// <summary>Random spread of start performance, in seconds.</summary>
    public double StartSpreadSeconds { get; init; } = 0.3;

    /// <summary>Time penalty applied for a jump start (a stand-in until the M7 penalty system).</summary>
    public double StartIncidentPenaltySeconds { get; init; } = 5.0;

    /// <summary>Chance a car stopping on track brings out a neutralisation (scaled by the
    /// circuit's safety-car likelihood).</summary>
    public double SafetyCarFromIncidentChance { get; init; } = 0.4;

    /// <summary>Of neutralisations, the share that are a virtual safety car rather than a full one.</summary>
    public double VirtualSafetyCarShare { get; init; } = 0.5;

    /// <summary>Of full (non-VSC) neutralisations, the share that escalate to a red flag.</summary>
    public double RedFlagShare { get; init; } = 0.1;

    /// <summary>How much slower a neutralised lap is than green (multiplier on base lap time).</summary>
    public double NeutralizationPaceFactor { get; init; } = 1.4;

    /// <summary>How many laps a safety car / VSC period lasts.</summary>
    public int NeutralizationLaps { get; init; } = 3;

    /// <summary>Gap, in seconds, between cars once the field is bunched behind the safety car.</summary>
    public double BunchGapSeconds { get; init; } = 0.6;

    /// <summary>Gap (seconds) under which a following car battles the car ahead. 0 disables traffic.</summary>
    public double CombatThresholdSeconds { get; init; } = 1.0;

    /// <summary>Lap-time a car loses stuck in the dirty air of the car ahead (held up).</summary>
    public double DirtyAirLossSeconds { get; init; } = 0.3;

    /// <summary>Overtake-chance boost from slipstream / DRS when within combat range.</summary>
    public double SlipstreamBoost { get; init; } = 0.4;

    /// <summary>How far ahead, in seconds, a successful overtaker ends up.</summary>
    public double PassMarginSeconds { get; init; } = 0.3;

    /// <summary>Time lost driving through the pit lane at the speed limit, vs. staying out (M7).</summary>
    public double PitLaneTimeLossSeconds { get; init; } = 20.0;

    /// <summary>Mean stationary time for a pit stop, in seconds.</summary>
    public double PitStopStationarySeconds { get; init; } = 2.6;

    /// <summary>Random spread of stationary time, in seconds (always adds — a stop can't gain time).</summary>
    public double PitStopSpreadSeconds { get; init; } = 0.4;

    /// <summary>Probability a pit stop is botched (cross-threaded wheel, stuck gun).</summary>
    public double SlowPitStopChance { get; init; } = 0.04;

    /// <summary>Extra time a botched pit stop costs, in seconds.</summary>
    public double SlowPitStopExtraSeconds { get; init; } = 6.0;

    /// <summary>How far, in laps, cars stagger their planned stops around the shared target so the
    /// field doesn't all pit on the same lap (the basis for undercut / overcut). 0 = everyone
    /// pits on the shared target lap (M7a behaviour).</summary>
    public int PitStaggerLaps { get; init; } = 3;

    /// <summary>How many laps before its planned stop a car will take the stop opportunistically
    /// while a neutralisation is out (a cheap safety-car stop).</summary>
    public int NeutralizationPitWindowLaps { get; init; } = 6;

    /// <summary>Fraction of the pit-lane time loss paid when pitting under a neutralisation — the
    /// field is slow, so the stop costs far less relative to staying out.</summary>
    public double NeutralizationPitDiscount { get; init; } = 0.45;

    /// <summary>How many off-track moments a driver is allowed before track limits draw a penalty.
    /// The strike after this count triggers a time penalty and resets the count (M7c).</summary>
    public int TrackLimitAllowance { get; init; } = 3;

    /// <summary>Time penalty, in seconds, added when a driver exceeds the track-limit allowance.</summary>
    public double TrackLimitPenaltySeconds { get; init; } = 5.0;

    /// <summary>Probability a pit stop ends in an unsafe release (a penalty), drawn once per stop.</summary>
    public double UnsafePitReleaseChance { get; init; } = 0.02;

    /// <summary>Time penalty, in seconds, added for an unsafe pit release.</summary>
    public double UnsafePitReleasePenaltySeconds { get; init; } = 5.0;

    /// <summary>Best-case lap-time gain, in seconds, from a fully productive practice program;
    /// scaled down by how much data the session actually yielded (M8).</summary>
    public double PracticeSetupGainSeconds { get; init; } = 0.30;

    /// <summary>Best-case reduction in driver-error chance (as a fraction) from a race-simulation
    /// practice program; scaled by session data quality (M8).</summary>
    public double PracticeErrorReduction { get; init; } = 0.25;

    /// <summary>How much a car's tyre-gentleness rating eases tyre wear (M14). 0 (default) means the
    /// car has no effect and wear is exactly as before — it is the R&amp;D-improvable tyre lever, so a
    /// carset opts in by setting it above zero; a gentler car then wears its tyres more slowly and is
    /// quicker deep into a stint.</summary>
    public double TyreGentlenessWearInfluence { get; init; }

    /// <summary>How much of the car's aerodynamic performance is lost per unit of accumulated race
    /// damage (R38). 0 (default) means incidents leave no lasting aero loss and the race is exactly
    /// as before — a carset opts in by setting it above zero, after which off-track moments and
    /// contact shed downforce (slower green laps) until the damage is repaired at a pit stop.</summary>
    public double DamageAeroLoss { get; init; }

    /// <summary>Extra stationary time, in seconds, a pit stop spends per unit of accumulated race
    /// damage to repair the car (R38). 0 (default) means repairs are free and instant, so a race is
    /// unchanged; a carset opts in by setting it above zero.</summary>
    public double DamageRepairSeconds { get; init; }
}
