using LightsToFlag.Core.Domain;

namespace LightsToFlag.Core.Simulation;

/// <summary>
/// Per-lap tyre wear model (percentage points added each lap). Wear rises with the
/// circuit's abrasiveness and the compound's softness, and falls with a smooth
/// driver and a car that is easy on its tyres.
/// </summary>
public static class TyreModel
{
    public static double WearPerLap(Competitor competitor, CircuitSpec circuit, TyreCompound tyre)
    {
        var severity = 0.5 + 0.15 * Rating(circuit.TyreWear);
        var care = (Rating(competitor.Driver.Smoothness) + Rating(competitor.Car.EaseOnTyres)) / 20.0; // 0..1
        var wear = severity * (1.4 - 0.8 * care) * CompoundFactor(tyre);
        return Math.Max(0.1, wear);
    }

    private static double CompoundFactor(TyreCompound tyre) => tyre switch
    {
        TyreCompound.Soft => 1.3,
        TyreCompound.Hard => 0.8,
        TyreCompound.Intermediate => 1.0,
        TyreCompound.Wet => 0.9,
        _ => 1.0,
    };

    private static double Rating(int oneToTen) => Math.Clamp(oneToTen, 1, 10);
}
