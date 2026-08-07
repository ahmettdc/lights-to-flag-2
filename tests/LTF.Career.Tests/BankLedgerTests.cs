using LTF.Domain;
using LTF.Domain.Management;
using LTF.Domain.Racing;
using Xunit;

namespace LTF.Career.Tests;

public class BankLedgerTests
{
    private static CreditProfile Profile(int offeredRate = 9, long limit = 50_000_000, long debt = 0) => new()
    {
        Score = 70, Tier = CreditTier.Strong, OfferedRatePercent = offeredRate, CreditLimit = limit, TotalDebt = debt,
    };

    private static Team Team(long balance = 0, IReadOnlyList<Loan>? loans = null) =>
        new()
        {
            Id = "alpha", Name = "Alpha",
            Car = new Car
            {
                Aerodynamics = new(70), Chassis = new(70), PowerUnit = new(70),
                TyreGentleness = new(70), Reliability = new(70),
            },
            Finances = new Finances { Balance = balance, Loans = loans ?? [] },
        };

    private static Loan Loan(long outstanding = 10_000_000, int rate = 10, int term = 5, int remaining = 5, int missed = 0) =>
        new()
        {
            Id = "loan-1", Principal = outstanding, AnnualRatePercent = rate, TermSeasons = term,
            SeasonsRemaining = remaining, OutstandingBalance = outstanding, MissedPayments = missed,
        };

    // A two-team carset with alpha carrying the given finances and a bank whose late fee is latePenalty%.
    private static Carset CarsetWith(Finances alphaFinances, int latePenalty = 10)
    {
        var carset = CareerFixtures.Carset();
        var alpha = carset.Teams[0] with { Finances = alphaFinances };
        var bank = carset.Rules.Bank with { LatePenaltyPercent = latePenalty };
        return carset with { Teams = [alpha, carset.Teams[1]], Rules = carset.Rules with { Bank = bank } };
    }

    private static Team Alpha(Carset carset) => carset.Teams[0];

    // --- Borrow ---

    [Fact]
    public void Borrowing_credits_the_balance_and_freezes_the_offered_rate()
    {
        var result = BankLedger.Borrow(Team(balance: 1_000_000), Profile(offeredRate: 9), amount: 20_000_000, termSeasons: 5, loanId: "loan-1");

        Assert.True(result.Approved);
        Assert.Equal(21_000_000, result.Team.Finances.Balance);
        var loan = Assert.Single(result.Team.Finances.Loans);
        Assert.Equal(9, loan.AnnualRatePercent); // the offered rate, now frozen
        Assert.Equal(20_000_000, loan.Principal);
        Assert.Equal(20_000_000, loan.OutstandingBalance);
        Assert.Equal(5, loan.SeasonsRemaining);
    }

    [Fact]
    public void Borrowing_beyond_the_headroom_is_declined_and_changes_nothing()
    {
        var team = Team(balance: 1_000_000);
        var result = BankLedger.Borrow(team, Profile(limit: 50_000_000), amount: 60_000_000, termSeasons: 5, loanId: "loan-1");

        Assert.Equal(LoanDecision.ExceedsHeadroom, result.Decision);
        Assert.False(result.Approved);
        Assert.Equal(team, result.Team); // untouched
        Assert.Empty(result.Team.Finances.Loans);
    }

    [Fact]
    public void A_non_positive_amount_or_term_is_declined()
    {
        Assert.Equal(LoanDecision.InvalidAmount, BankLedger.Borrow(Team(), Profile(), 0, 5, "l").Decision);
        Assert.Equal(LoanDecision.InvalidTerm, BankLedger.Borrow(Team(), Profile(), 5_000_000, 0, "l").Decision);
    }

    [Fact]
    public void A_loan_signed_later_at_worse_credit_carries_a_higher_frozen_rate()
    {
        // First loan drawn while credit is good (rate 8), then a second while the world has worsened (rate 20).
        var afterFirst = BankLedger.Borrow(Team(balance: 0), Profile(offeredRate: 8, limit: 80_000_000), 20_000_000, 5, "loan-1").Team;
        var afterSecond = BankLedger.Borrow(afterFirst, Profile(offeredRate: 20, limit: 80_000_000, debt: 20_000_000), 20_000_000, 5, "loan-2").Team;

        Assert.Equal(8, afterSecond.Finances.Loans[0].AnnualRatePercent);  // the first loan's rate stayed frozen
        Assert.Equal(20, afterSecond.Finances.Loans[1].AnnualRatePercent); // the second took the worse offer
    }

    // --- SettleSeason ---

    [Fact]
    public void Settlement_accrues_interest_and_pays_down_the_loan()
    {
        var carset = CarsetWith(new Finances { Balance = 8_000_000, Loans = [Loan()] });

        var settled = BankLedger.SettleSeason(carset);
        var loan = Assert.Single(Alpha(settled.Carset).Finances.Loans);

        // interest 1M + principal 2M = 3M instalment; balance 8M → 5M; outstanding 10M → 8M; a season ticks off.
        Assert.Equal(5_000_000, Alpha(settled.Carset).Finances.Balance);
        Assert.Equal(8_000_000, loan.OutstandingBalance);
        Assert.Equal(4, loan.SeasonsRemaining);
        Assert.Equal(0, loan.MissedPayments);
        Assert.Empty(settled.Missed);
    }

    [Fact]
    public void A_loan_is_repaid_and_drops_off_over_its_term()
    {
        var carset = CarsetWith(new Finances { Balance = 1_000_000_000, Loans = [Loan()] });

        for (var season = 0; season < 5; season++)
        {
            carset = BankLedger.SettleSeason(carset).Carset;
        }

        Assert.Empty(Alpha(carset).Finances.Loans); // fully repaid
        // 10M principal + (1.0+0.8+0.6+0.4+0.2)M interest = 13M total serviced.
        Assert.Equal(1_000_000_000 - 13_000_000, Alpha(carset).Finances.Balance);
    }

    [Fact]
    public void A_missed_instalment_capitalises_and_is_recorded()
    {
        var carset = CarsetWith(new Finances { Balance = 1_000_000, Loans = [Loan()] }, latePenalty: 10);

        var settled = BankLedger.SettleSeason(carset);
        var loan = Assert.Single(Alpha(settled.Carset).Finances.Loans);

        // instalment 3M unaffordable on 1M → miss. late fee 300k; capitalised = 1M interest + 300k = 1.3M.
        Assert.Equal(1, loan.MissedPayments);
        Assert.Equal(11_300_000, loan.OutstandingBalance);
        Assert.Equal(1_000_000, Alpha(settled.Carset).Finances.Balance); // nothing paid
        var miss = Assert.Single(settled.Missed);
        Assert.Equal("alpha", miss.TeamId);
        Assert.Equal("loan-1", miss.LoanId);
        Assert.Equal(1, miss.MissedPaymentsTotal);
        Assert.Equal(1_300_000, miss.AmountCapitalised);
    }

    [Fact]
    public void A_team_with_no_loans_is_left_untouched_by_settlement()
    {
        var carset = CarsetWith(new Finances { Balance = 5_000_000 });
        var before = Alpha(carset);

        var settled = BankLedger.SettleSeason(carset);

        Assert.Equal(before, Alpha(settled.Carset)); // byte-identical finances
        Assert.Empty(settled.Missed);
    }

    [Fact]
    public void Settlement_is_deterministic()
    {
        var carset = CarsetWith(new Finances { Balance = 8_000_000, Loans = [Loan()] });

        Assert.Equal(BankLedger.SettleSeason(carset).Carset.Teams[0], BankLedger.SettleSeason(carset).Carset.Teams[0]);
    }
}
