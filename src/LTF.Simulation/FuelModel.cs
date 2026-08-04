using LTF.Domain.Racing;

namespace LTF.Simulation;

/// <summary>
/// Fuel load and its cost. A full tank is heavy and slow; the car speeds up through a
/// stint as it burns off. Fuel is modelled as a fraction of a full race load (1 = brimmed,
/// 0 = running on fumes).
/// </summary>
public static class FuelModel
{
    /// <summary>Seconds a lap loses to carrying <paramref name="fuelFraction"/> of a full load.</summary>
    public static double Penalty(double fuelFraction, BalanceCoefficients balance) =>
        balance.FuelLoadPenaltySeconds * Math.Clamp(fuelFraction, 0.0, 1.0);

    /// <summary>Fuel left after burning one lap's worth over a <paramref name="totalLaps"/> race.</summary>
    public static double Burn(double fuelFraction, int totalLaps) =>
        totalLaps <= 0 ? fuelFraction : Math.Max(0.0, fuelFraction - (1.0 / totalLaps));
}
