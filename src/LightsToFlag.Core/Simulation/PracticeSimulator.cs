using LightsToFlag.Core.Domain;

namespace LightsToFlag.Core.Simulation;

/// <summary>
/// Simulates a practice session: each competitor converts running laps into
/// setup quality (0..1). The ceiling a team can reach is set by the driver's
/// feedback and the car's setup aptitude; laps approach that ceiling with
/// diminishing returns. The returned competitors carry the earned
/// <see cref="Competitor.SetupQuality"/> into qualifying and the race.
/// </summary>
public static class PracticeSimulator
{
    private const int DefaultLaps = 20;
    private const double LapTimeConstant = 12.0; // laps to reach ~63% of the ceiling

    public static IReadOnlyList<Competitor> Run(
        IReadOnlyList<Competitor> competitors,
        Coefficients coeff,
        IRandom rng,
        IReadOnlyDictionary<string, int>? lapsByCompetitor = null)
    {
        var result = new List<Competitor>(competitors.Count);
        foreach (var competitor in competitors)
        {
            var laps = lapsByCompetitor is not null && lapsByCompetitor.TryGetValue(competitor.Id, out var custom)
                ? Math.Max(0, custom)
                : DefaultLaps;

            result.Add(competitor with { SetupQuality = ComputeSetup(competitor, coeff, laps, rng) });
        }

        return result;
    }

    public static double ComputeSetup(Competitor competitor, Coefficients coeff, int laps, IRandom rng)
    {
        var capability = (Rating(competitor.Driver.Feedback) + Rating(competitor.Car.Setup)) / 20.0; // 0..1
        var effectiveness = coeff.SetupEffectiveness > 0 ? coeff.SetupEffectiveness : 1.0;
        var ceiling = Math.Clamp(0.4 + 0.6 * capability * effectiveness, 0.0, 1.0);

        var progress = 1.0 - Math.Exp(-laps / LapTimeConstant);
        var quality = ceiling * progress;

        // A little session-to-session variance, but never enough to break monotonicity meaningfully.
        quality += rng.NextGaussian() * 0.01;
        return Math.Clamp(quality, 0.0, 1.0);
    }

    private static double Rating(int oneToTen) => Math.Clamp(oneToTen, 1, 10);
}
