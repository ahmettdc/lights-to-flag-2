using LTF.Domain.Common;
using LTF.Domain.Racing;

namespace LTF.Simulation;

/// <summary>
/// Tyre performance and degradation. Softer compounds are quicker but wear faster;
/// wear costs lap time gently at first and then falls off a "cliff" near the end of a
/// set's life. Deterministic — the randomness that matters (a stint's length, when to
/// stop) lives in strategy and the race loop, not here.
/// </summary>
public static class TyreModel
{
    private const double CliffPoint = 0.85;

    /// <summary>Intrinsic dry-pace offset by compound, in seconds (relative to Medium).</summary>
    public static double CompoundDryOffset(TyreCompound compound) => compound switch
    {
        TyreCompound.Soft => -0.6,
        TyreCompound.Medium => 0.0,
        TyreCompound.Hard => 0.5,
        TyreCompound.Intermediate => 2.5,
        TyreCompound.Wet => 4.0,
        _ => 0.0,
    };

    private static double CompoundWearFactor(TyreCompound compound) => compound switch
    {
        TyreCompound.Soft => 1.4,
        TyreCompound.Medium => 1.0,
        TyreCompound.Hard => 0.7,
        TyreCompound.Intermediate => 1.1,
        TyreCompound.Wet => 1.0,
        _ => 1.0,
    };

    /// <summary>How much wear (0–1) this set takes in one lap here, for this driver and car. The
    /// car's tyre-gentleness eases wear only when the carset opts in via
    /// <see cref="BalanceCoefficients.TyreGentlenessWearInfluence"/> (0 by default → the car has no
    /// effect and this is byte-identical to before M14).</summary>
    public static double WearRate(
        TyreState tyre, Circuit circuit, DriverAttributes driver, Rating carTyreGentleness, BalanceCoefficients balance)
    {
        var stress = 0.5 + circuit.TyreStress.Normalized;              // 0.5 … 1.5
        var management = 0.7 + (driver.TyreManagement.Normalized * 0.6) // driver's care
                             + (carTyreGentleness.Normalized * balance.TyreGentlenessWearInfluence); // the car's (M14)
        return balance.TyreWearPerLap * stress * CompoundWearFactor(tyre.Compound) / management;
    }

    /// <summary>Age the set by one lap, accumulating wear (clamped at fully worn).</summary>
    public static TyreState Advance(
        TyreState tyre, Circuit circuit, DriverAttributes driver, Rating carTyreGentleness, BalanceCoefficients balance) =>
        tyre with
        {
            Age = tyre.Age + 1,
            Wear = Math.Min(1.0, tyre.Wear + WearRate(tyre, circuit, driver, carTyreGentleness, balance)),
        };

    /// <summary>Seconds this set adds to the lap right now (compound + wear + graining).</summary>
    public static double TimeDelta(TyreState tyre, Circuit circuit) =>
        CompoundDryOffset(tyre.Compound) + WearPenalty(tyre.Wear) + Graining(tyre, circuit);

    private static double WearPenalty(double wear)
    {
        var linear = wear * 2.5;
        var cliff = wear > CliffPoint ? (wear - CliffPoint) * 25.0 : 0.0;
        return linear + cliff;
    }

    private static double Graining(TyreState tyre, Circuit circuit)
    {
        if (tyre.Age == 0)
        {
            return 0.0;
        }

        var amplitude = tyre.Compound switch
        {
            TyreCompound.Soft => 0.4,
            TyreCompound.Medium => 0.2,
            _ => 0.05,
        } * (0.5 + circuit.TyreStress.Normalized);

        // A bump that peaks a few laps in and fades as the surface scrubs in.
        var x = tyre.Age - 3.0;
        return amplitude * Math.Exp(-(x * x) / 8.0);
    }
}
