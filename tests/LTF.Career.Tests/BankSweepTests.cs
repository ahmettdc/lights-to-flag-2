using LTF.Domain.Racing;
using Xunit;

namespace LTF.Career.Tests;

public class BankSweepTests
{
    // A season carset (alpha the clear winner) with an economy that pays real revenue and an active bank.
    private static Domain.Carset Carset()
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 4) with { PlayerTeamId = "alpha" };
        var rules = carset.Rules with
        {
            Economy = new EconomyRules { PrizeMoney = [50_000_000, 30_000_000], TvIncome = 20_000_000 },
            Bank = new BankRules
            {
                BaseRatePercent = 6, MaxRiskPremiumPercent = 24, MaxLoanToRevenuePercent = 200,
                LatePenaltyPercent = 10, AssetSeizureAfterMisses = 2, InsolvencyAfterMisses = 4,
                InsolvencyPointsPenalty = 15,
            },
        };
        return carset with { Rules = rules };
    }

    [Fact]
    public void A_serviceable_loan_is_drawn_and_repaid_over_its_term()
    {
        var report = BankSweep.Run(Carset(), seasons: 7, seed: 7, amount: 20_000_000, termSeasons: 5);

        Assert.Equal("alpha", report.BorrowerTeamId);
        Assert.Equal(20_000_000, report.AmountBorrowed);
        Assert.True(report.FrozenRatePercent >= 6);   // at least the base rate
        Assert.True(report.PeakDebt >= 20_000_000);    // the debt was carried
        Assert.Equal(0, report.FinalDebt);             // and cleared
        Assert.True(report.LoanRepaid);
        Assert.Equal(0, report.SeasonsMissed);         // a winning team services it comfortably
        Assert.Equal(0, report.PointsDocked);
    }

    [Fact]
    public void The_sweep_is_deterministic()
    {
        var carset = Carset();

        Assert.Equal(
            BankSweep.Run(carset, seasons: 7, seed: 4, amount: 20_000_000, termSeasons: 5),
            BankSweep.Run(carset, seasons: 7, seed: 4, amount: 20_000_000, termSeasons: 5));
    }

    [Fact]
    public void With_no_active_bank_nothing_is_borrowed()
    {
        // The bare season carset has no economy and no bank block → no headroom, nothing drawn.
        var report = BankSweep.Run(CareerFixtures.SeasonCarset(rounds: 4), seasons: 5, seed: 7, amount: 20_000_000, termSeasons: 5);

        Assert.Equal(0, report.AmountBorrowed);
        Assert.Equal(0, report.PeakDebt);
        Assert.False(report.LoanRepaid);
    }
}
