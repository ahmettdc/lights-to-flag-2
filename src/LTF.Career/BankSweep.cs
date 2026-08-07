using LTF.Domain;
using LTF.Domain.Management;
using LTF.Domain.Racing;

namespace LTF.Career;

/// <summary>
/// The aggregate result of a banking sweep (ADR-0029): a borrowing team's loan trajectory over many
/// seasons — the amount drawn and its frozen rate, how the debt peaked and where it ended, and how far
/// enforcement climbed. It answers the ROADMAP's banking question — a loan is serviced deterministically
/// and either repaid or held under active enforcement, never a runaway and never a game-over.
/// </summary>
public sealed record BankSweepReport
{
    public required int Seasons { get; init; }
    public required string BorrowerTeamId { get; init; }

    /// <summary>How much the borrower actually drew (0 if it could not, e.g. no headroom).</summary>
    public required long AmountBorrowed { get; init; }

    /// <summary>The rate the loan was signed at (frozen for its life); 0 when nothing was borrowed.</summary>
    public required int FrozenRatePercent { get; init; }

    /// <summary>The highest total debt the borrower carried across the sweep.</summary>
    public required long PeakDebt { get; init; }

    /// <summary>Debt still outstanding at the end of the last swept season.</summary>
    public required long FinalDebt { get; init; }

    /// <summary>Seasons in which the borrower missed at least one instalment.</summary>
    public required int SeasonsMissed { get; init; }

    /// <summary>Forced asset sales the enforcement ladder triggered against the borrower.</summary>
    public required int Seizures { get; init; }

    /// <summary>Terminal (insolvency) escalations against the borrower.</summary>
    public required int InsolvencyEvents { get; init; }

    /// <summary>Total constructor points docked from the borrower by the terminal penalty.</summary>
    public required int PointsDocked { get; init; }

    /// <summary>Whether the loan was fully repaid (no debt left) by the end.</summary>
    public required bool LoanRepaid { get; init; }
}

/// <summary>
/// Runs many headless seasons of a career that takes a bank loan to check the debt model holds up
/// (ADR-0029): each season it simulates the championship (<see cref="SeasonSimulator"/>), settles the
/// economy (<see cref="EconomyLedger"/>), services the loans (<see cref="BankLedger.SettleSeason"/>) and
/// runs the enforcement ladder (<see cref="BankEnforcement"/>), then rolls the records forward
/// (<see cref="CareerRollover"/>, which keeps the settled finances and loans). The borrower draws once,
/// after the first season has given it revenue and a standing to be scored on. Fully deterministic — each
/// season's seed is derived from the base seed — so the same inputs reproduce the same report. No I/O; the
/// CLI (<c>ltf sweep</c>) wraps this.
/// </summary>
public static class BankSweep
{
    public static BankSweepReport Run(
        Carset carset, int seasons, int seed, long amount, int termSeasons, string? borrowerId = null)
    {
        if (seasons < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(seasons), seasons, "must be at least one season");
        }

        var borrower = borrowerId
            ?? (carset.PlayerTeamId.Length > 0 ? carset.PlayerTeamId : carset.Teams[0].Id);

        var current = carset;
        var borrowed = false;
        long amountBorrowed = 0;
        var frozenRate = 0;
        long peakDebt = 0;
        long finalDebt = 0;
        var seasonsMissed = 0;
        var seizures = 0;
        var insolvencyEvents = 0;
        var pointsDocked = 0;

        for (var season = 0; season < seasons; season++)
        {
            var result = SeasonSimulator.Run(current, SeasonSeed(seed, season));
            current = EconomyLedger.SettleSeason(current, result).Carset;

            var settlement = BankLedger.SettleSeason(current);
            current = settlement.Carset;
            var enforcement = BankEnforcement.Enforce(current, settlement.Missed);
            current = enforcement.Carset;

            if (settlement.Missed.Any(m => string.Equals(m.TeamId, borrower, StringComparison.Ordinal)))
            {
                seasonsMissed++;
            }

            foreach (var action in enforcement.Actions)
            {
                if (!string.Equals(action.TeamId, borrower, StringComparison.Ordinal))
                {
                    continue;
                }

                if (action.Step >= EnforcementStep.AssetSeizure)
                {
                    seizures++;
                }

                if (action.Step == EnforcementStep.Insolvency)
                {
                    insolvencyEvents++;
                }

                pointsDocked += action.PointsDocked;
            }

            // Draw the loan once the first settled season has given the team revenue and a standing.
            if (!borrowed)
            {
                (current, amountBorrowed, frozenRate) = TryBorrow(current, result.Standings, borrower, amount, termSeasons);
                borrowed = true;
            }

            var debt = TeamOf(current, borrower)?.Finances.TotalDebt ?? 0;
            peakDebt = Math.Max(peakDebt, debt);
            finalDebt = debt;

            current = CareerRollover.Apply(current, result);
        }

        return new BankSweepReport
        {
            Seasons = seasons,
            BorrowerTeamId = borrower,
            AmountBorrowed = amountBorrowed,
            FrozenRatePercent = frozenRate,
            PeakDebt = peakDebt,
            FinalDebt = finalDebt,
            SeasonsMissed = seasonsMissed,
            Seizures = seizures,
            InsolvencyEvents = insolvencyEvents,
            PointsDocked = pointsDocked,
            LoanRepaid = borrowed && amountBorrowed > 0 && finalDebt <= 0,
        };
    }

    private static (Carset Carset, long Amount, int Rate) TryBorrow(
        Carset carset, Standings standings, string borrowerId, long amount, int termSeasons)
    {
        var team = TeamOf(carset, borrowerId);
        if (team is null)
        {
            return (carset, 0, 0);
        }

        var profile = CreditProfile.For(
            team, standings, BoardOf(carset, borrowerId), carset.Rules.Bank, carset.Rules.Economy);
        var draw = Math.Min(amount, profile.Headroom);
        if (draw <= 0)
        {
            return (carset, 0, 0);
        }

        var result = BankLedger.Borrow(team, profile, draw, termSeasons, "loan-1");
        if (!result.Approved)
        {
            return (carset, 0, 0);
        }

        return (ReplaceTeam(carset, result.Team), draw, result.Loan!.AnnualRatePercent);
    }

    private static Carset ReplaceTeam(Carset carset, Team updated)
    {
        var teams = carset.Teams
            .Select(t => string.Equals(t.Id, updated.Id, StringComparison.Ordinal) ? updated : t)
            .ToList();
        return carset with { Teams = teams };
    }

    private static Team? TeamOf(Carset carset, string teamId)
    {
        foreach (var team in carset.Teams)
        {
            if (string.Equals(team.Id, teamId, StringComparison.Ordinal))
            {
                return team;
            }
        }

        return null;
    }

    private static TeamBoard? BoardOf(Carset carset, string teamId)
    {
        foreach (var board in carset.Boards)
        {
            if (string.Equals(board.TeamId, teamId, StringComparison.Ordinal))
            {
                return board;
            }
        }

        return null;
    }

    // A deterministic per-season seed from the base seed and season index (mirrors EconomySweep's mixing).
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
