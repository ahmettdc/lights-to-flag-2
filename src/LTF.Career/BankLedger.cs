using LTF.Domain;
using LTF.Domain.Management;
using LTF.Domain.Racing;

namespace LTF.Career;

/// <summary>Why a borrowing request was approved or turned down (ADR-0029).</summary>
public enum LoanDecision
{
    Approved,

    /// <summary>The requested amount was zero or negative.</summary>
    InvalidAmount,

    /// <summary>The requested term was zero or negative.</summary>
    InvalidTerm,

    /// <summary>The amount would push total debt past the team's credit limit.</summary>
    ExceedsHeadroom,
}

/// <summary>The outcome of a borrowing request (ADR-0029): the decision, the team (updated on approval,
/// unchanged on a decline) and the new loan when one was drawn.</summary>
public sealed record BorrowResult
{
    public required LoanDecision Decision { get; init; }

    /// <summary>The team after borrowing (credited balance + the new loan), or the original team on a decline.</summary>
    public required Team Team { get; init; }

    /// <summary>The loan that was drawn, or null on a decline.</summary>
    public Loan? Loan { get; init; }

    public bool Approved => Decision == LoanDecision.Approved;
}

/// <summary>A single missed instalment recorded during settlement (ADR-0029) — the hook the enforcement
/// (icra) ladder escalates on. Carries who missed, on which loan, the running miss count and how much
/// unpaid interest plus late fee capitalised into the debt.</summary>
public sealed record MissedPayment
{
    public required string TeamId { get; init; }
    public required string LoanId { get; init; }

    /// <summary>Total instalments missed on this loan after this one (drives the enforcement thresholds).</summary>
    public required int MissedPaymentsTotal { get; init; }

    /// <summary>Unpaid interest plus late fee added to the outstanding balance this season.</summary>
    public required long AmountCapitalised { get; init; }
}

/// <summary>The result of settling the season's loans (ADR-0029): the carset with every team's loans
/// advanced and balances charged, and the missed instalments raised this season.</summary>
public sealed record BankSettlement
{
    public required Carset Carset { get; init; }

    /// <summary>Missed instalments this season (empty when everyone paid, or nobody has any debt).</summary>
    public IReadOnlyList<MissedPayment> Missed { get; init; } = [];
}

/// <summary>
/// The season's banking book (ADR-0029): <see cref="Borrow"/> draws a loan against a team's live credit
/// headroom, freezing the offered rate into the loan, and <see cref="SettleSeason"/> services every
/// outstanding loan at rollover — accruing interest at each loan's own frozen rate, deducting the season's
/// instalment, paying down the principal, and recording a missed instalment (with a capitalising late fee)
/// when the balance can't cover it. Pure, deterministic arithmetic (no RNG); a team that has never borrowed
/// carries no loans, so settlement leaves it untouched and the books byte-identical.
/// </summary>
public static class BankLedger
{
    /// <summary>Draw a loan for <paramref name="team"/> against its current <paramref name="profile"/>: the
    /// amount must be positive, the term positive, and the amount within the profile's headroom. On approval
    /// the balance is credited and the loan captures the profile's <em>offered</em> rate as its frozen rate.</summary>
    public static BorrowResult Borrow(
        Team team, CreditProfile profile, long amount, int termSeasons, string loanId, string lender = "")
    {
        if (amount <= 0)
        {
            return Decline(team, LoanDecision.InvalidAmount);
        }

        if (termSeasons <= 0)
        {
            return Decline(team, LoanDecision.InvalidTerm);
        }

        if (amount > profile.Headroom)
        {
            return Decline(team, LoanDecision.ExceedsHeadroom);
        }

        var loan = new Loan
        {
            Id = loanId,
            Lender = lender,
            Principal = amount,
            AnnualRatePercent = profile.OfferedRatePercent, // frozen at signing — the offer is dynamic, the loan is not
            TermSeasons = termSeasons,
            SeasonsRemaining = termSeasons,
            OutstandingBalance = amount,
        };

        var finances = team.Finances with
        {
            Balance = team.Finances.Balance + amount,
            Loans = [.. team.Finances.Loans, loan],
        };

        return new BorrowResult
        {
            Decision = LoanDecision.Approved,
            Team = team with { Finances = finances },
            Loan = loan,
        };
    }

    private static BorrowResult Decline(Team team, LoanDecision decision) =>
        new() { Decision = decision, Team = team };

    /// <summary>Service every team's loans for the season and collect the missed instalments. Teams with no
    /// loans are returned untouched, so a carset where nobody has borrowed is byte-identical.</summary>
    public static BankSettlement SettleSeason(Carset carset)
    {
        var missed = new List<MissedPayment>();
        var teams = new List<Team>(carset.Teams.Count);
        foreach (var team in carset.Teams)
        {
            teams.Add(SettleTeam(team, carset.Rules.Bank, missed));
        }

        return new BankSettlement { Carset = carset with { Teams = teams }, Missed = missed };
    }

    private static Team SettleTeam(Team team, BankRules bank, List<MissedPayment> missed)
    {
        if (team.Finances.Loans.Count == 0)
        {
            return team; // never borrowed — no money moves, byte-identical
        }

        var balance = team.Finances.Balance;
        var loans = new List<Loan>(team.Finances.Loans.Count);
        foreach (var loan in team.Finances.Loans)
        {
            var (advanced, newBalance, capitalised) = SettleLoan(loan, balance, bank);
            balance = newBalance;

            if (capitalised is { } amount)
            {
                missed.Add(new MissedPayment
                {
                    TeamId = team.Id,
                    LoanId = advanced.Id,
                    MissedPaymentsTotal = advanced.MissedPayments,
                    AmountCapitalised = amount,
                });
            }

            if (!advanced.IsSettled)
            {
                loans.Add(advanced); // a fully repaid loan drops off the books
            }
        }

        return team with { Finances = team.Finances with { Balance = balance, Loans = loans } };
    }

    // Service one loan for a season. Returns the advanced loan, the balance after the instalment, and — on a
    // miss — the amount that capitalised into the debt (null when the instalment was paid).
    private static (Loan Loan, long Balance, long? Capitalised) SettleLoan(Loan loan, long balance, BankRules bank)
    {
        var interest = loan.OutstandingBalance * loan.AnnualRatePercent / 100;

        // Straight-line to zero over the remaining term; the final (or an overdue) season demands the lot.
        var principalPortion = loan.SeasonsRemaining > 1
            ? loan.OutstandingBalance / loan.SeasonsRemaining
            : loan.OutstandingBalance;
        var instalment = interest + principalPortion;
        var seasonsRemaining = Math.Max(0, loan.SeasonsRemaining - 1);

        if (balance >= instalment)
        {
            var paid = loan with
            {
                OutstandingBalance = loan.OutstandingBalance - principalPortion,
                SeasonsRemaining = seasonsRemaining,
            };
            return (paid, balance - instalment, null);
        }

        // Missed: nothing is paid, so the unpaid interest and a late fee capitalise — the debt grows.
        var lateFee = instalment * bank.LatePenaltyPercent / 100;
        var capitalised = interest + lateFee;
        var missedLoan = loan with
        {
            OutstandingBalance = loan.OutstandingBalance + capitalised,
            SeasonsRemaining = seasonsRemaining,
            MissedPayments = loan.MissedPayments + 1,
        };
        return (missedLoan, balance, capitalised);
    }
}
