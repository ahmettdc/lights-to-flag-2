using LTF.Domain.Racing;

namespace LTF.Simulation.Racing;

/// <summary>
/// How hard a car is being run. It trades pace against reliability: <see cref="Push"/> is
/// faster but wears components and raises failure risk; <see cref="Conserve"/> is the
/// opposite; <see cref="Standard"/> is neutral. M5b wires the mechanism and leaves every
/// car in <see cref="Standard"/> — choosing when to push or save is a strategy concern (M7).
/// </summary>
public enum EngineMode
{
    Conserve,
    Standard,
    Push,
}

/// <summary>Pure, deterministic effects of an <see cref="EngineMode"/> on pace and risk.</summary>
internal static class EngineModes
{
    /// <summary>Lap-time delta in seconds (negative is faster). Standard is 0.</summary>
    public static double PaceDelta(EngineMode mode, BalanceCoefficients balance) => mode switch
    {
        EngineMode.Push => -balance.EngineModePaceGainSeconds,
        EngineMode.Conserve => balance.EngineModePaceGainSeconds,
        _ => 0.0,
    };

    /// <summary>Multiplier applied to failure risk and component wear. Standard is 1.0.</summary>
    public static double RiskFactor(EngineMode mode, BalanceCoefficients balance) => mode switch
    {
        EngineMode.Push => balance.EngineModeRiskFactor,
        EngineMode.Conserve => 1.0 / balance.EngineModeRiskFactor,
        _ => 1.0,
    };
}
