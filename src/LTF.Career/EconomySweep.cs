using LTF.Domain;

namespace LTF.Career;

/// <summary>Totals accumulated for one team over an economy sweep (M13): its balance trajectory across
/// the swept seasons and how often it ran in the red or breached the cost cap.</summary>
public sealed record TeamEconomyStat
{
    public required string TeamId { get; init; }

    /// <summary>Balance at the end of the last swept season.</summary>
    public required long FinalBalance { get; init; }

    /// <summary>Lowest end-of-season balance seen across the sweep.</summary>
    public required long MinBalance { get; init; }

    /// <summary>Highest end-of-season balance seen across the sweep.</summary>
    public required long MaxBalance { get; init; }

    /// <summary>Number of season-ends the team finished with a negative balance.</summary>
    public required int SeasonsInDebt { get; init; }

    /// <summary>Cost-cap breach penalties raised against the team across the sweep.</summary>
    public required int CostCapPenalties { get; init; }
}

/// <summary>
/// The aggregate result of an economy sweep (M13): whether the money model balances over many seasons.
/// It answers the ROADMAP's economy question — no team is driven into permanent bankruptcy, and cash
/// does not pile up without bound — so the economy coefficients can be judged against a real trajectory.
/// </summary>
public sealed record EconomySweepReport
{
    public required int Seasons { get; init; }

    /// <summary>Per-team totals, ordered by final balance (richest first).</summary>
    public required IReadOnlyList<TeamEconomyStat> Teams { get; init; }

    /// <summary>The poorest team's final balance — negative means someone ended in the red.</summary>
    public required long MinFinalBalance { get; init; }

    /// <summary>The richest team's final balance — the ceiling on how far cash piled up.</summary>
    public required long MaxFinalBalance { get; init; }

    /// <summary>How many teams ended the sweep with a negative balance.</summary>
    public required int BankruptTeams { get; init; }
}

/// <summary>
/// Runs many headless seasons of a career's economy to check that it balances over time (M13): each
/// season it simulates the championship (<see cref="SeasonSimulator"/>), settles the books
/// (<see cref="EconomyLedger"/>) and rolls the records forward (<see cref="CareerRollover"/>), which
/// keeps the settled finances, tracking every team's balance across the years. Fully deterministic —
/// each season's seed is derived from the base seed, so the same inputs reproduce the same report.
/// No I/O; the CLI (<c>ltf sweep</c>) wraps this and formats the output.
/// </summary>
public static class EconomySweep
{
    public static EconomySweepReport Run(Carset carset, int seasons, int seed)
    {
        if (seasons < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(seasons), seasons, "must be at least one season");
        }

        var min = new Dictionary<string, long>(StringComparer.Ordinal);
        var max = new Dictionary<string, long>(StringComparer.Ordinal);
        var inDebt = new Dictionary<string, int>(StringComparer.Ordinal);
        var penalties = new Dictionary<string, int>(StringComparer.Ordinal);
        var final = new Dictionary<string, long>(StringComparer.Ordinal);

        var current = carset;
        for (var season = 0; season < seasons; season++)
        {
            var result = SeasonSimulator.Run(current, SeasonSeed(seed, season));
            var settlement = EconomyLedger.SettleSeason(current, result);

            foreach (var team in settlement.Carset.Teams)
            {
                var balance = team.Finances.Balance;
                final[team.Id] = balance;
                min[team.Id] = min.TryGetValue(team.Id, out var lo) ? Math.Min(lo, balance) : balance;
                max[team.Id] = max.TryGetValue(team.Id, out var hi) ? Math.Max(hi, balance) : balance;
                if (balance < 0)
                {
                    inDebt[team.Id] = inDebt.GetValueOrDefault(team.Id) + 1;
                }
            }

            foreach (var penalty in settlement.Penalties)
            {
                penalties[penalty.TeamId] = penalties.GetValueOrDefault(penalty.TeamId) + 1;
            }

            // Roll the records forward for the next season; CareerRollover keeps the settled finances.
            current = CareerRollover.Apply(settlement.Carset, result);
        }

        var teams = carset.Teams
            .Select(t => new TeamEconomyStat
            {
                TeamId = t.Id,
                FinalBalance = final.GetValueOrDefault(t.Id),
                MinBalance = min.GetValueOrDefault(t.Id),
                MaxBalance = max.GetValueOrDefault(t.Id),
                SeasonsInDebt = inDebt.GetValueOrDefault(t.Id),
                CostCapPenalties = penalties.GetValueOrDefault(t.Id),
            })
            .OrderByDescending(s => s.FinalBalance)
            .ThenBy(s => s.TeamId, StringComparer.Ordinal)
            .ToList();

        return new EconomySweepReport
        {
            Seasons = seasons,
            Teams = teams,
            MinFinalBalance = teams.Count > 0 ? teams.Min(s => s.FinalBalance) : 0,
            MaxFinalBalance = teams.Count > 0 ? teams.Max(s => s.FinalBalance) : 0,
            BankruptTeams = teams.Count(s => s.FinalBalance < 0),
        };
    }

    // A deterministic per-season seed from the base seed and season index (mirrors BalanceSweep's mixing).
    private static int SeasonSeed(int baseSeed, int season)
    {
        unchecked
        {
            var h = (uint)baseSeed;
            h = (h ^ (uint)season) * 2654435761u;
            h ^= h >> 16;
            return (int)h;
        }
    }
}
