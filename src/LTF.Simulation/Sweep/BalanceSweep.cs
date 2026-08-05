using LTF.Domain;
using LTF.Simulation.Qualifying;
using LTF.Simulation.Racing;

namespace LTF.Simulation.Sweep;

/// <summary>
/// Simulates many headless seasons of a carset to measure how the current coefficients play out
/// (M10). For each season it runs every calendar round — qualifying for the grid, then the race —
/// sums championship points, and crowns the season's champion; across all seasons it aggregates the
/// balance metrics the ROADMAP calls for. Fully deterministic: every race seed is derived from the
/// base seed, season and round, so the same inputs reproduce the same report. No I/O — the CLI
/// (<c>ltf sweep</c>, M10c) wraps this and formats the output.
/// </summary>
public static class BalanceSweep
{
    public static SweepReport Run(Carset carset, int seasons, int seed)
    {
        if (seasons < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(seasons), seasons, "must be at least one season");
        }

        var entries = EntryList.Build(carset);
        var entriesById = entries.ToDictionary(c => c.Id, StringComparer.Ordinal);
        var circuitsById = carset.Circuits.ToDictionary(c => c.Id, StringComparer.Ordinal);

        var points = entries.ToDictionary(c => c.Id, _ => 0, StringComparer.Ordinal);
        var wins = entries.ToDictionary(c => c.Id, _ => 0, StringComparer.Ordinal);
        var titles = entries.ToDictionary(c => c.Id, _ => 0, StringComparer.Ordinal);
        var starts = entries.ToDictionary(c => c.Id, _ => 0, StringComparer.Ordinal);
        var retirements = entries.ToDictionary(c => c.Id, _ => 0, StringComparer.Ordinal);

        var races = 0;
        var carRaces = 0;
        var dnfs = 0;
        var neutralisations = 0;
        var pitStops = 0;

        for (var season = 0; season < seasons; season++)
        {
            var seasonPoints = entries.ToDictionary(c => c.Id, _ => 0, StringComparer.Ordinal);

            foreach (var round in carset.Calendar)
            {
                if (!circuitsById.TryGetValue(round.CircuitId, out var circuit))
                {
                    throw new InvalidOperationException(
                        $"round {round.Round} references unknown circuit '{round.CircuitId}'.");
                }

                var raceSeed = RaceSeed(seed, season, round.Round);
                var quali = QualifyingSimulator.Run(circuit, entries, carset.Rules, carset.Balance, raceSeed);
                var grid = quali.StartingOrder.Select(id => entriesById[id]).ToList();
                var format = new RaceFormat { PoleSitterId = quali.PoleCompetitorId, IsSprint = round.IsSprint };
                var result = RaceSimulator.Run(
                    circuit, grid, carset.Rules, carset.Balance, raceSeed,
                    regulations: carset.Regulations, format: format);

                races++;
                foreach (var e in result.Classification)
                {
                    points[e.CompetitorId] += e.Points;
                    seasonPoints[e.CompetitorId] += e.Points;
                    starts[e.CompetitorId]++;
                    carRaces++;
                    if (e.Status != FinishStatus.Finished)
                    {
                        retirements[e.CompetitorId]++;
                        dnfs++;
                    }

                    if (e.Position == 1 && e.Status == FinishStatus.Finished)
                    {
                        wins[e.CompetitorId]++;
                    }
                }

                foreach (var ev in result.Events)
                {
                    if (ev.Kind is RaceEventKind.SafetyCar or RaceEventKind.VirtualSafetyCar or RaceEventKind.RedFlag)
                    {
                        neutralisations++;
                    }
                    else if (ev.Kind == RaceEventKind.Pit)
                    {
                        pitStops++;
                    }
                }
            }

            // The season champion is the most points, ties broken by entry order (deterministic).
            string? champion = null;
            var bestPoints = -1;
            foreach (var c in entries)
            {
                if (seasonPoints[c.Id] > bestPoints)
                {
                    bestPoints = seasonPoints[c.Id];
                    champion = c.Id;
                }
            }

            if (champion is not null)
            {
                titles[champion]++;
            }
        }

        var stats = entries
            .Select(c => new CompetitorSweepStat
            {
                CompetitorId = c.Id,
                Titles = titles[c.Id],
                Wins = wins[c.Id],
                Points = points[c.Id],
                Starts = starts[c.Id],
                Retirements = retirements[c.Id],
            })
            .OrderByDescending(x => x.Titles)
            .ThenByDescending(x => x.Points)
            .ThenBy(x => x.CompetitorId, StringComparer.Ordinal)
            .ToList();

        return new SweepReport
        {
            Seasons = seasons,
            Rounds = carset.Calendar.Count,
            Races = races,
            Competitors = stats,
            RetirementRate = carRaces > 0 ? (double)dnfs / carRaces : 0.0,
            SafetyCarsPerRace = races > 0 ? (double)neutralisations / races : 0.0,
            AveragePitStopsPerCar = carRaces > 0 ? (double)pitStops / carRaces : 0.0,
        };
    }

    /// <summary>A deterministic per-race seed from the base seed, season and round — well mixed so
    /// distinct races vary while the whole sweep stays reproducible from the base seed.</summary>
    private static int RaceSeed(int baseSeed, int season, int round)
    {
        unchecked
        {
            var h = (uint)baseSeed;
            h = (h ^ (uint)season) * 2654435761u;
            h = (h ^ (uint)round) * 2654435761u;
            h ^= h >> 16;
            return (int)h;
        }
    }
}
