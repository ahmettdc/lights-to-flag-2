using LTF.Domain.Common;
using LTF.Domain.Racing;

namespace LTF.Simulation;

/// <summary>
/// How weather and the chosen tyre interact, and how the track dries or floods over a
/// session. The tyre-vs-wetness curves cross over — slicks in the dry, intermediates in
/// the mixed band, full wets in heavy rain — which is what makes tyre calls matter later.
/// </summary>
public static class WeatherModel
{
    /// <summary>Seconds lost from running <paramref name="compound"/> at this wetness.</summary>
    public static double ConditionPenalty(TyreCompound compound, double wetness)
    {
        wetness = Math.Clamp(wetness, 0.0, 1.0);
        return compound switch
        {
            TyreCompound.Intermediate => Math.Abs(wetness - 0.45) * 9.0,
            TyreCompound.Wet => wetness >= 0.85 ? (wetness - 0.85) * 4.0 : (0.85 - wetness) * 7.0,
            _ => Slick(wetness),
        };
    }

    // Slicks are fine on a dry track and increasingly hopeless as water builds.
    private static double Slick(double wetness) =>
        wetness <= 0.05 ? 0.0 : wetness * wetness * 34.0;

    /// <summary>
    /// Advance the track one step: a bounded random walk whose size grows with the
    /// circuit's weather variability. Deterministic given the supplied <see cref="IRandom"/>.
    /// </summary>
    public static TrackConditions Evolve(TrackConditions track, Circuit circuit, IRandom rng)
    {
        var step = 0.02 + (circuit.WeatherVariability.Normalized * 0.06);
        var next = track.Wetness + (rng.NextGaussian() * step);
        return new TrackConditions(Math.Clamp(next, 0.0, 1.0));
    }
}
