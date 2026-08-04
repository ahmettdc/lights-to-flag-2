using LTF.Domain.Racing;

namespace LTF.Simulation.Laps;

/// <summary>
/// The core lap-time model: circuit reference pace scaled by how well a car and driver
/// suit the track, then perturbed by per-sector noise. Deterministic given the supplied
/// <see cref="IRandom"/>. Tyres, fuel and weather layer on top in later milestones (M4+);
/// this milestone establishes the pace backbone and the determinism guarantee.
/// </summary>
public static class LapTimeModel
{
    // Sector split of a lap (sums to 1). Even-ish thirds; per-circuit shaping comes later.
    private static readonly double[] SectorFraction = [0.34, 0.33, 0.33];

    // A competitor whose blended pace equals ReferencePace laps at the circuit's base time;
    // PaceSpread sets how many seconds separate the field.
    private const double ReferencePace = 0.85;
    private const double PaceSpread = 0.25;

    public static SectorTimes Simulate(
        Circuit circuit, Car car, DriverAttributes driver, BalanceCoefficients balance, IRandom rng)
    {
        var pace = (balance.CarPaceWeight * CarScore(car, circuit))
                   + (balance.DriverPaceWeight * DriverScore(driver));

        var lapCore = circuit.BaseLapTimeSeconds * (1.0 + (PaceSpread * (ReferencePace - pace)));

        // Less consistent drivers scatter more lap to lap.
        var noiseScale = balance.RandomnessSpreadSeconds * (1.2 - driver.Consistency.Normalized);

        var s1 = Sector(lapCore, SectorFraction[0], noiseScale, rng);
        var s2 = Sector(lapCore, SectorFraction[1], noiseScale, rng);
        var s3 = Sector(lapCore, SectorFraction[2], noiseScale, rng);
        return new SectorTimes(s1, s2, s3);
    }

    public static SectorTimes Simulate(
        Circuit circuit, Competitor competitor, BalanceCoefficients balance, IRandom rng) =>
        Simulate(circuit, competitor.Car, competitor.Driver.Attributes, balance, rng);

    private static double Sector(double lapCore, double fraction, double noiseScale, IRandom rng) =>
        (lapCore * fraction) + (rng.NextGaussian() * noiseScale * fraction);

    /// <summary>How well the car suits this circuit, 0–1, weighting power vs downforce by track.</summary>
    private static double CarScore(Car car, Circuit circuit)
    {
        var wPower = circuit.PowerSensitivity.Normalized;
        var wDownforce = circuit.DownforceSensitivity.Normalized;
        const double wMechanical = 0.5;
        var total = wPower + wDownforce + wMechanical;

        return ((car.PowerUnit.Normalized * wPower)
                + (car.Aerodynamics.Normalized * wDownforce)
                + (car.Chassis.Normalized * wMechanical)) / total;
    }

    /// <summary>The driver's contribution to pace, 0–1 (outright speed with a consistency lean).</summary>
    private static double DriverScore(DriverAttributes driver) =>
        (driver.Pace.Normalized * 0.7) + (driver.Consistency.Normalized * 0.3);
}
