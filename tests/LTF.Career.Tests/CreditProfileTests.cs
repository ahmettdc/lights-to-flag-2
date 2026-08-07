using LTF.Domain.Common;
using LTF.Domain.Management;
using LTF.Domain.Racing;
using Xunit;

namespace LTF.Career.Tests;

public class CreditProfileTests
{
    private static readonly BankRules ActiveBank = new()
    {
        BaseRatePercent = 6,
        MaxRiskPremiumPercent = 24,
        MaxLoanToRevenuePercent = 200,
        LatePenaltyPercent = 10,
    };

    private static readonly EconomyRules Economy = new() { TvIncome = 10_000_000 };

    private static Car Car(int flat) => new()
    {
        Aerodynamics = new Rating(flat), Chassis = new Rating(flat), PowerUnit = new Rating(flat),
        TyreGentleness = new Rating(flat), Reliability = new Rating(flat),
    };

    private static Team Team(
        int car = 70, long balance = 0, long prize = 0, long sponsor = 0, IReadOnlyList<Loan>? loans = null) => new()
    {
        Id = "t", Name = "T", Car = Car(car),
        Finances = new Finances
        {
            Balance = balance, PrizeMoney = prize, SponsorIncome = sponsor, Loans = loans ?? [],
        },
    };

    private static Loan Loan(long outstanding, int missed = 0) => new()
    {
        Id = "loan-1", Principal = outstanding, AnnualRatePercent = 6, TermSeasons = 5,
        SeasonsRemaining = 5, OutstandingBalance = outstanding, MissedPayments = missed,
    };

    // A standings table of fieldSize teams with "t" sitting at the given position.
    private static Standings StandingsWith(int position, int fieldSize)
    {
        var constructors = new List<ConstructorStanding>();
        for (var p = 1; p <= fieldSize; p++)
        {
            constructors.Add(new ConstructorStanding
            {
                Position = p, TeamId = p == position ? "t" : $"o{p}", Points = (fieldSize - p) * 10, Wins = 0,
            });
        }

        return new Standings { Drivers = [], Constructors = constructors };
    }

    private static TeamBoard Board(int confidence, int firingRisk) => new()
    {
        TeamId = "t",
        Metrics = PressureMetrics.Neutral with { BoardConfidence = new Pressure(confidence) },
        FiringRisk = new Pressure(firingRisk),
    };

    // A title-winning, cash-rich, board-backed team.
    private static CreditProfile Strong() => CreditProfile.For(
        Team(car: 90, balance: 20_000_000, prize: 30_000_000, sponsor: 10_000_000),
        StandingsWith(position: 1, fieldSize: 10),
        Board(confidence: 80, firingRisk: 5),
        ActiveBank, Economy);

    // A back-of-the-grid, broke, sack-threatened team.
    private static CreditProfile Weak() => CreditProfile.For(
        Team(car: 45, balance: -15_000_000, prize: 2_000_000, sponsor: 1_000_000),
        StandingsWith(position: 10, fieldSize: 10),
        Board(confidence: 20, firingRisk: 70),
        ActiveBank, Economy);

    [Fact]
    public void A_strong_team_outscores_a_weak_one()
    {
        Assert.True(Strong().Score > Weak().Score);
        Assert.Equal(CreditTier.Excellent, Strong().Tier);
        Assert.Equal(CreditTier.Poor, Weak().Tier);
    }

    [Fact]
    public void The_offered_rate_falls_as_the_score_rises()
    {
        Assert.True(Strong().OfferedRatePercent < Weak().OfferedRatePercent);
        // Best credit is quoted the base rate; the premium only bites the weak.
        Assert.True(Strong().OfferedRatePercent >= ActiveBank.BaseRatePercent);
        Assert.True(Weak().OfferedRatePercent <= ActiveBank.BaseRatePercent + ActiveBank.MaxRiskPremiumPercent);
    }

    [Fact]
    public void The_credit_limit_rises_with_the_score()
    {
        // Same revenue, different creditworthiness → the stronger team may borrow more.
        var rich = Team(car: 95, balance: 40_000_000, prize: 30_000_000, sponsor: 10_000_000);
        var poor = Team(car: 40, balance: -20_000_000, prize: 30_000_000, sponsor: 10_000_000);

        var strong = CreditProfile.For(rich, StandingsWith(1, 10), Board(85, 5), ActiveBank, Economy);
        var weak = CreditProfile.For(poor, StandingsWith(10, 10), Board(15, 80), ActiveBank, Economy);

        Assert.True(strong.Score > weak.Score);
        Assert.True(strong.CreditLimit > weak.CreditLimit);
    }

    [Fact]
    public void The_credit_limit_rises_with_revenue()
    {
        var lean = CreditProfile.For(
            Team(car: 80, prize: 5_000_000, sponsor: 2_000_000), StandingsWith(4, 10), null, ActiveBank, Economy);
        var flush = CreditProfile.For(
            Team(car: 80, prize: 40_000_000, sponsor: 20_000_000), StandingsWith(4, 10), null, ActiveBank, Economy);

        Assert.True(flush.CreditLimit > lean.CreditLimit);
    }

    [Fact]
    public void Missed_payments_lower_the_score()
    {
        // Identical teams and identical outstanding debt — only the repayment record differs.
        var clean = CreditProfile.For(
            Team(car: 70, balance: 5_000_000, prize: 20_000_000, loans: [Loan(10_000_000, missed: 0)]),
            StandingsWith(5, 10), null, ActiveBank, Economy);
        var defaulted = CreditProfile.For(
            Team(car: 70, balance: 5_000_000, prize: 20_000_000, loans: [Loan(10_000_000, missed: 3)]),
            StandingsWith(5, 10), null, ActiveBank, Economy);

        Assert.True(defaulted.Score < clean.Score);
        Assert.Equal(clean.TotalDebt, defaulted.TotalDebt); // same debt, isolating the history effect
    }

    [Fact]
    public void An_inert_bank_offers_no_credit()
    {
        var profile = CreditProfile.For(
            Team(car: 90, balance: 50_000_000, prize: 40_000_000, sponsor: 20_000_000),
            StandingsWith(1, 10), Board(90, 0), new BankRules(), Economy);

        Assert.False(new BankRules().IsActive);
        Assert.Equal(0, profile.CreditLimit);
        Assert.Equal(0, profile.Headroom);
        Assert.False(profile.CanBorrow);
    }

    [Fact]
    public void Headroom_is_the_limit_less_the_outstanding_debt()
    {
        var team = Team(car: 80, balance: 5_000_000, prize: 30_000_000, sponsor: 10_000_000,
            loans: [Loan(12_000_000)]);
        var profile = CreditProfile.For(team, StandingsWith(3, 10), Board(70, 10), ActiveBank, Economy);

        Assert.Equal(12_000_000, profile.TotalDebt);
        Assert.Equal(profile.CreditLimit - 12_000_000, profile.Headroom);
        Assert.True(profile.CanBorrow); // still room under the limit
    }

    [Fact]
    public void A_team_with_no_board_still_gets_a_scored_profile()
    {
        var profile = CreditProfile.For(
            Team(car: 75, balance: 3_000_000, prize: 15_000_000), StandingsWith(6, 10), null, ActiveBank, Economy);

        Assert.InRange(profile.Score, 0, 100);
        Assert.True(profile.CreditLimit > 0);
    }

    [Fact]
    public void The_board_signal_moves_the_score()
    {
        // A backed board should not score worse than a hostile one, all else equal.
        var team = Team(car: 70, balance: 2_000_000, prize: 15_000_000);
        var backed = CreditProfile.For(team, StandingsWith(5, 10), Board(90, 0), ActiveBank, Economy);
        var hostile = CreditProfile.For(team, StandingsWith(5, 10), Board(10, 90), ActiveBank, Economy);

        Assert.True(backed.Score > hostile.Score);
    }

    [Fact]
    public void The_score_is_always_in_range()
    {
        // Extremes on every axis must still land inside 0–100 and never quote below the base rate.
        var maxed = CreditProfile.For(
            Team(car: 100, balance: long.MaxValue / 4, prize: 100_000_000, sponsor: 100_000_000),
            StandingsWith(1, 20), Board(100, 0), ActiveBank, Economy);
        var floored = CreditProfile.For(
            Team(car: 1, balance: -100_000_000, prize: 1, sponsor: 0, loans: [Loan(1, missed: 99)]),
            StandingsWith(20, 20), Board(0, 100), ActiveBank, Economy);

        Assert.InRange(maxed.Score, 0, 100);
        Assert.InRange(floored.Score, 0, 100);
        Assert.True(floored.OfferedRatePercent >= ActiveBank.BaseRatePercent);
    }

    [Fact]
    public void The_profile_is_deterministic()
    {
        Assert.Equal(Strong(), Strong()); // record equality — same world in, same profile out
    }
}
