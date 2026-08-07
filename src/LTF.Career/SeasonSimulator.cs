using LTF.Domain;
using LTF.Domain.Racing;
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
/// From M15 the loop threads the carset so a between-rounds progression can evolve it (in-season
/// updates, test days); with no progression the season is byte-identical to the M11 behaviour.
/// From M21 the per-round body is a public <see cref="RunRound"/> the live career advances one round
/// at a time; <see cref="RunCore"/> calls it in its loop, so the whole-season result is unchanged.
/// </summary>
public static class SeasonSimulator
{
    /// <summary>Run one full season, returning the championship result. The car and roster are fixed
    /// for the whole season (M11) — byte-identical to before M15.</summary>
    public static SeasonResult Run(Carset carset, int seed) => RunCore(carset, seed, null).Result;

    /// <summary>Run one full season with a between-rounds progression (M15): after each round the
    /// progression may return an evolved carset that the remaining rounds are contested with, and the
    /// final carset rides back alongside the result (Shape B). Byte-identical to <see cref="Run"/> when
    /// the progression changes nothing.</summary>
    public static SeasonProgress RunProgressed(Carset carset, int seed, IBetweenRounds progression) =>
        RunCore(carset, seed, progression);

    /// <summary>
    /// Run a single round: qualify, apply the round's grid penalty, then race (M21). The building block the
    /// whole-season <see cref="Run"/> loops over and the live career advances one round at a time.
    /// Deterministic in <paramref name="roundSeed"/> (derive it with <see cref="RoundSeed"/>).
    /// <paramref name="gridPenalty"/> is supplied by the caller on purpose: component grid penalties are a
    /// <em>season-scoped</em> quantity (<see cref="ComponentPenalties.ForSeason"/> keyed by round), not a
    /// function of a single round's carset — recomputing one here from an evolved carset would diverge from
    /// the whole-season run and break byte-identity.
    /// </summary>
    public static RoundOutcome RunRound(
        Carset carset, CalendarRound round, int roundSeed, IReadOnlyDictionary<string, int> gridPenalty)
    {
        var circuit = FindCircuit(carset, round);

        var entries = SeasonEntries.Build(carset);
        var entriesById = entries.ToDictionary(c => c.Id, StringComparer.Ordinal);
        var quali = QualifyingSimulator.Run(circuit, entries, carset.Rules, carset.Balance, roundSeed);
        var grid = quali.StartingOrder.Select(id => entriesById[id]).ToList();
        if (gridPenalty.Count > 0)
        {
            grid = GridOrder.WithPenalties(grid, gridPenalty).ToList();
        }

        var format = new RaceFormat { PoleSitterId = quali.PoleCompetitorId, IsSprint = round.IsSprint };
        var result = RaceSimulator.Run(
            circuit, grid, carset.Rules, carset.Balance, roundSeed,
            regulations: carset.Regulations, format: format);

        return new RoundOutcome(result, quali.PoleCompetitorId);
    }

    private static SeasonProgress RunCore(Carset carset, int seed, IBetweenRounds? between)
    {
        var rounds = new List<RaceResult>(carset.Calendar.Count);
        var poleSitters = new List<string?>(carset.Calendar.Count);

        // Per-round component-allocation grid penalties (M15). Season-scoped: computed once from the
        // season-start carset and indexed by round. Empty every round when no allocation is configured or
        // every car fits its quota, so the grid is left exactly as qualifying set it.
        var penalties = ComponentPenalties.ForSeason(carset);

        // The carset is threaded through the season so a progression can evolve it between rounds. With
        // no progression `current` never changes, so rebuilding the entry list each round yields entries
        // value-equal to building it once — the season stays byte-identical to the M11 Run.
        var current = carset;
        var roundCount = carset.Calendar.Count;
        var index = 0;
        foreach (var round in carset.Calendar)
        {
            var outcome = RunRound(current, round, RoundSeed(seed, round.Round), penalties[index]);
            rounds.Add(outcome.Result);
            poleSitters.Add(outcome.PoleSitterId);

            if (between is not null)
            {
                var nextDate = index + 1 < carset.Calendar.Count
                    ? carset.Calendar[index + 1].Date
                    : (DateOnly?)null;
                current = between.AfterRound(current, new BetweenRoundsContext
                {
                    Round = round,
                    RoundIndex = index,
                    RoundCount = roundCount,
                    NextRoundDate = nextDate,
                    Seed = RoundSeed(seed, round.Round),
                });
            }

            index++;
        }

        // Standings read only the roster (teams, drivers), which a progression never changes, so the
        // initial carset is the correct and byte-identical basis — matching the M11 Run exactly.
        var standings = ChampionshipStandings.From(carset, rounds);
        return new SeasonProgress
        {
            Carset = current,
            Result = new SeasonResult { Standings = standings, Rounds = rounds, PoleSitters = poleSitters },
        };
    }

    /// <summary>A deterministic per-round seed from the season seed and round number — well mixed so
    /// distinct rounds vary while the whole season stays reproducible from the season seed. Public so the
    /// live career (M21) derives the same seed the whole-season run uses.</summary>
    public static int RoundSeed(int seasonSeed, int round)
    {
        unchecked
        {
            var h = (uint)seasonSeed;
            h = (h ^ (uint)round) * 2654435761u;
            h ^= h >> 16;
            return (int)h;
        }
    }

    private static Circuit FindCircuit(Carset carset, CalendarRound round)
    {
        foreach (var circuit in carset.Circuits)
        {
            if (string.CompareOrdinal(circuit.Id, round.CircuitId) == 0)
            {
                return circuit;
            }
        }

        throw new InvalidOperationException(
            $"round {round.Round} references unknown circuit '{round.CircuitId}'.");
    }
}
