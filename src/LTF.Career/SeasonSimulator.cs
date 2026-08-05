using LTF.Domain;
using LTF.Simulation;
using LTF.Simulation.Qualifying;
using LTF.Simulation.Racing;

namespace LTF.Career;

/// <summary>
/// Runs one full season of a carset (M11): each calendar round is qualified for the grid and then
/// raced, and the results are folded into the championship <see cref="Standings"/>. Fully
/// deterministic — each round's seed is derived from the season seed and the round number, so the
/// same carset and seed reproduce the same season. Pure and I/O-free, as the Career layer must be;
/// it reuses the M8/M9 qualifying and race simulators exactly as the M10 sweep does, for one season.
/// </summary>
public static class SeasonSimulator
{
    public static SeasonResult Run(Carset carset, int seed)
    {
        var entries = EntryList.Build(carset);
        var entriesById = entries.ToDictionary(c => c.Id, StringComparer.Ordinal);
        var circuitsById = carset.Circuits.ToDictionary(c => c.Id, StringComparer.Ordinal);

        var rounds = new List<RaceResult>(carset.Calendar.Count);
        foreach (var round in carset.Calendar)
        {
            if (!circuitsById.TryGetValue(round.CircuitId, out var circuit))
            {
                throw new InvalidOperationException(
                    $"round {round.Round} references unknown circuit '{round.CircuitId}'.");
            }

            var roundSeed = RoundSeed(seed, round.Round);
            var quali = QualifyingSimulator.Run(circuit, entries, carset.Rules, carset.Balance, roundSeed);
            var grid = quali.StartingOrder.Select(id => entriesById[id]).ToList();
            var format = new RaceFormat { PoleSitterId = quali.PoleCompetitorId, IsSprint = round.IsSprint };
            var result = RaceSimulator.Run(
                circuit, grid, carset.Rules, carset.Balance, roundSeed,
                regulations: carset.Regulations, format: format);
            rounds.Add(result);
        }

        var standings = ChampionshipStandings.From(carset, rounds);
        return new SeasonResult { Standings = standings, Rounds = rounds };
    }

    /// <summary>A deterministic per-round seed from the season seed and round number — well mixed so
    /// distinct rounds vary while the whole season stays reproducible from the season seed.</summary>
    private static int RoundSeed(int seasonSeed, int round)
    {
        unchecked
        {
            var h = (uint)seasonSeed;
            h = (h ^ (uint)round) * 2654435761u;
            h ^= h >> 16;
            return (int)h;
        }
    }
}
