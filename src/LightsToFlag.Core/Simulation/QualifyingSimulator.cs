using LightsToFlag.Core.Domain;

namespace LightsToFlag.Core.Simulation;

/// <summary>
/// Simulates qualifying: each competitor sets a best low-fuel flying lap (best of
/// a few attempts, using qualifying skills and fresh soft tyres), and the grid is
/// ordered fastest-first. Ties break on the raw time then, deterministically, on
/// competitor id so a run is fully reproducible.
/// </summary>
public static class QualifyingSimulator
{
    private const double QualifyingFuelLaps = 2.0;

    public static QualifyingResult Run(
        IReadOnlyList<Competitor> competitors,
        CircuitSpec circuit,
        Coefficients coeff,
        RulesSet rules,
        IRandom rng,
        double wetness = 0.0)
    {
        var attempts = Math.Max(1, Math.Min(3, rules.QualifyingMaxLapsPerSession is > 0 and var m ? m : 3));

        var timed = new List<(string Id, double Best)>(competitors.Count);
        foreach (var competitor in competitors)
        {
            var ctx = new LapContext
            {
                Competitor = competitor,
                Circuit = circuit,
                Coefficients = coeff,
                FuelLapsRemaining = QualifyingFuelLaps,
                TyreWearPercent = 0,
                Tyre = wetness > 0.3 ? TyreCompound.Wet : TyreCompound.Soft,
                Wetness = wetness,
                QualifyingTrim = true,
            };

            var best = double.MaxValue;
            for (var i = 0; i < attempts; i++)
            {
                best = Math.Min(best, LapTimeCalculator.WithNoise(ctx, rng));
            }

            timed.Add((competitor.Id, best));
        }

        var ordered = timed
            .OrderBy(t => t.Best)
            .ThenBy(t => t.Id, StringComparer.Ordinal)
            .ToList();

        var grid = new List<QualifyingEntry>(ordered.Count);
        for (var i = 0; i < ordered.Count; i++)
        {
            grid.Add(new QualifyingEntry
            {
                CompetitorId = ordered[i].Id,
                GridPosition = i + 1,
                BestLapSeconds = ordered[i].Best,
            });
        }

        return new QualifyingResult { Grid = grid };
    }
}
