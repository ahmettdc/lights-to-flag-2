using LTF.Domain;
using LTF.Domain.Management;

namespace LTF.Career;

/// <summary>One season's snapshot of the player team's board (M17).</summary>
public sealed record BossSeasonRecord
{
    public required int Season { get; init; }
    public required int BoardConfidence { get; init; }
    public required int FiringRisk { get; init; }
    public required int ProposalsPassed { get; init; }
    public required int ContractsSigned { get; init; }
}

/// <summary>Per-team totals at the end of a boss career sweep (M17): the final car and finances, for the
/// byte-identity guard against <see cref="ResearchSweep"/>.</summary>
public sealed record BossTeamStat
{
    public required string TeamId { get; init; }
    public required int FinalOverall { get; init; }
    public required long FinalBalance { get; init; }
}

/// <summary>The result of a boss career sweep (M17): the player team's board trajectory across the seasons,
/// each team's final car and finances, and whether the principal was ever near the sack.</summary>
public sealed record BossCareerSweepReport
{
    public required string PlayerTeamId { get; init; }
    public required int Seasons { get; init; }
    public required IReadOnlyList<BossSeasonRecord> PlayerBoard { get; init; }
    public required IReadOnlyList<BossTeamStat> Teams { get; init; }
    public required bool EverAtRisk { get; init; }
    public required int FinalBoardConfidence { get; init; }
}

/// <summary>
/// Runs a headless Team-Principal career (M17), the counterpart to <see cref="ResearchSweep"/> and
/// <see cref="EconomySweep"/>. Each season it develops R&amp;D under the player's directive, settles the books
/// (folding R&amp;D spend into the cost cap), applies any cost-cap points to the constructors' table, lets the
/// board judge the penalty-adjusted season and evolve its pressure and firing risk, then applies the boss's
/// contract and regulation decisions and rolls the records forward — writing the board back after
/// settlement so nothing overwrites the evolution. With no boards, no player team and no proposals it drives
/// no player decisions and leaves the shared engine untouched, reproducing the research sweep's car
/// trajectory. Fully deterministic (each season's seed derives from the base seed); no I/O.
/// </summary>
public static class BossCareerSweep
{
    private const int AtRiskThreshold = 25; // firing risk at or above this counts as "near the sack"

    public static BossCareerSweepReport Run(Carset carset, int seasons, int seed, IBossPolicy? policy = null)
    {
        if (seasons < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(seasons), seasons, "must be at least one season");
        }

        var boss = policy ?? new BossPolicy();
        var current = BoardReview.SetObjectives(carset);
        var records = new List<BossSeasonRecord>(seasons);

        for (var season = 0; season < seasons; season++)
        {
            var seasonSeed = SeasonSeed(seed, season);

            // Develop R&D under the player's directive during the season.
            var progression = new RndProgression(seasonSeed, boss.Directives(current));
            var progress = SeasonSimulator.RunProgressed(current, seasonSeed, progression);

            // Settle the books, folding each team's R&D spend into the cost cap.
            var rndSpend = progression.Developments.ToDictionary(d => d.TeamId, d => d.BudgetSpent, StringComparer.Ordinal);
            var settlement = EconomyLedger.SettleSeason(progress.Carset, progress.Result, rndSpend);

            // Apply the deferred cost-cap points to the constructors' table the board will judge.
            var adjusted = ConstructorPenalties.Apply(progress.Result.Standings, settlement.Penalties);

            // The board judges the penalty-adjusted season and evolves its pressure and firing risk.
            var assessed = BoardReview.Assess(settlement.Carset, adjusted, seasonSeed);

            // The boss's decisions: sign the offered drivers, vote on regulation.
            var (afterSigning, signed) = ApplyOffers(assessed, boss.ContractOffers(assessed));
            var passed = ResolveProposals(afterSigning, boss, seasonSeed);

            records.Add(RecordFor(season, afterSigning, passed, signed));

            // Roll contracts and records forward for the next season; the evolved boards ride along.
            var advanced = ContractLedger.AdvanceSeason(afterSigning);
            current = CareerRollover.Apply(advanced, progress.Result);
        }

        return BuildReport(carset.PlayerTeamId, seasons, records, current);
    }

    private static (Carset Carset, int Signed) ApplyOffers(Carset carset, IReadOnlyList<ContractOffer> offers)
    {
        var current = carset;
        var signed = 0;
        foreach (var offer in offers)
        {
            var result = ContractSigning.Sign(current, offer);
            current = result.Carset;
            if (result.Signed)
            {
                signed++;
            }
        }

        return (current, signed);
    }

    private static int ResolveProposals(Carset carset, IBossPolicy boss, int seed)
    {
        var passed = 0;
        foreach (var proposal in carset.RegulationProposals)
        {
            if (RegulationBallot.Resolve(carset, proposal, seed, boss.Vote(carset, proposal)).Passed)
            {
                passed++;
            }
        }

        return passed;
    }

    private static BossSeasonRecord RecordFor(int season, Carset carset, int proposalsPassed, int signed)
    {
        var board = PlayerBoard(carset);
        return new BossSeasonRecord
        {
            Season = season,
            BoardConfidence = board?.Metrics.BoardConfidence.Value ?? 0,
            FiringRisk = board?.FiringRisk.Value ?? 0,
            ProposalsPassed = proposalsPassed,
            ContractsSigned = signed,
        };
    }

    private static BossCareerSweepReport BuildReport(
        string playerTeamId, int seasons, List<BossSeasonRecord> records, Carset final)
    {
        var teams = final.Teams
            .Select(t => new BossTeamStat { TeamId = t.Id, FinalOverall = t.Car.Overall, FinalBalance = t.Finances.Balance })
            .OrderBy(t => t.TeamId, StringComparer.Ordinal)
            .ToList();

        return new BossCareerSweepReport
        {
            PlayerTeamId = playerTeamId,
            Seasons = seasons,
            PlayerBoard = records,
            Teams = teams,
            EverAtRisk = records.Any(r => r.FiringRisk >= AtRiskThreshold),
            FinalBoardConfidence = records.Count > 0 ? records[^1].BoardConfidence : 0,
        };
    }

    private static TeamBoard? PlayerBoard(Carset carset)
    {
        foreach (var board in carset.Boards)
        {
            if (string.CompareOrdinal(board.TeamId, carset.PlayerTeamId) == 0)
            {
                return board;
            }
        }

        return null;
    }

    // A deterministic per-season seed from the base seed and season index (mirrors the other sweeps).
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
