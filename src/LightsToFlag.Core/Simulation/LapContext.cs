using LightsToFlag.Core.Domain;

namespace LightsToFlag.Core.Simulation;

/// <summary>
/// Everything needed to price a single lap for one competitor. Immutable input
/// to <see cref="LapTimeCalculator"/>.
/// </summary>
public readonly record struct LapContext
{
    public required Competitor Competitor { get; init; }
    public required CircuitSpec Circuit { get; init; }
    public required Coefficients Coefficients { get; init; }

    /// <summary>Laps' worth of fuel still on board (drives the weight penalty).</summary>
    public double FuelLapsRemaining { get; init; }

    /// <summary>Current tyre wear, 0 (fresh) … 100 (worn out).</summary>
    public double TyreWearPercent { get; init; }

    public TyreCompound Tyre { get; init; }

    /// <summary>Track wetness, 0 (dry) … 1 (fully wet).</summary>
    public double Wetness { get; init; }

    /// <summary>True for a low-fuel qualifying effort (uses qualifying skills).</summary>
    public bool QualifyingTrim { get; init; }
}
