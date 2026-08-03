using LightsToFlag.Core.Domain;

namespace LightsToFlag.Core.Simulation;

/// <summary>
/// Generates a per-lap wetness timeline (0 dry … 1 fully wet). Most races stay
/// dry; a rain event's chance scales with the circuit's weather changeability and
/// the carset's poor-weather likelihood. Deterministic given the RNG.
/// </summary>
public static class WeatherModel
{
    public static double[] Generate(CircuitSpec circuit, Coefficients coeff, int laps, IRandom rng)
    {
        var wetness = new double[Math.Max(1, laps)];
        if (!Chance(coeff.PoorWeatherLikelihood, circuit.WeatherChangeability, rng))
        {
            return wetness; // stays dry
        }

        // A rain band: ramp up from a start lap to a peak, then ease off.
        var start = rng.Next(wetness.Length);
        var rampUp = 3 + rng.Next(5);
        var peak = 0.4 + 0.6 * rng.NextDouble();
        var duration = rampUp + 4 + rng.Next(Math.Max(1, wetness.Length / 3));

        for (var lap = start; lap < wetness.Length && lap < start + duration; lap++)
        {
            var t = lap - start;
            var level = t < rampUp
                ? peak * (t + 1) / rampUp
                : peak * Math.Max(0.0, 1.0 - (t - rampUp) / (double)Math.Max(1, duration - rampUp));
            wetness[lap] = Math.Clamp(level, 0.0, 1.0);
        }

        return wetness;
    }

    private static bool Chance(double poorWeatherLikelihood, int changeability, IRandom rng)
    {
        var p = Math.Clamp(poorWeatherLikelihood, 0.0, 1.0) * (Math.Clamp(changeability, 0, 10) / 10.0) * 0.4;
        return rng.NextDouble() < p;
    }
}
