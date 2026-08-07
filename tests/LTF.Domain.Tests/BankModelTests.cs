using LTF.Domain.Management;
using LTF.Domain.Racing;
using Xunit;

namespace LTF.Domain.Tests;

/// <summary>The banking domain records (ADR-0029): loans, total debt, and the inert-by-default bank rules.</summary>
public class BankModelTests
{
    private static Loan Loan(long outstanding) => new()
    {
        Id = "loan-1",
        Principal = 10_000_000,
        AnnualRatePercent = 8,
        TermSeasons = 4,
        SeasonsRemaining = 4,
        OutstandingBalance = outstanding,
    };

    [Fact]
    public void Total_debt_sums_outstanding_balances()
    {
        var finances = new Finances
        {
            Loans = new[] { Loan(6_000_000), Loan(2_500_000) with { Id = "loan-2" } },
        };

        Assert.Equal(8_500_000L, finances.TotalDebt);
    }

    [Fact]
    public void A_team_that_never_borrowed_carries_no_debt()
    {
        var finances = new Finances();

        Assert.Empty(finances.Loans);
        Assert.Equal(0L, finances.TotalDebt);
    }

    [Fact]
    public void A_loan_is_settled_once_nothing_is_owed()
    {
        Assert.False(Loan(1).IsSettled);
        Assert.True(Loan(0).IsSettled);
    }

    [Fact]
    public void Bank_rules_are_inert_by_default()
    {
        var bank = new BankRules();

        Assert.False(bank.IsActive);
        Assert.Equal(0, bank.MaxLoanToRevenuePercent);
        Assert.Equal(0, bank.BaseRatePercent);
    }

    [Fact]
    public void Rules_default_to_an_inert_bank()
    {
        var rules = new RulesSet { SeriesName = "S", Points = new PointsScheme { RacePoints = [25] } };

        Assert.False(rules.Bank.IsActive);
    }

    [Fact]
    public void An_active_bank_offers_credit()
    {
        var bank = new BankRules { MaxLoanToRevenuePercent = 200, BaseRatePercent = 6 };

        Assert.True(bank.IsActive);
    }
}
