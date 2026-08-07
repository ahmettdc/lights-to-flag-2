using LTF.Domain;
using LTF.Domain.Common;
using LTF.Domain.Management;
using LTF.Domain.Racing;
using Xunit;

namespace LTF.Career.Tests;

public class BankEnforcementTests
{
    private static Loan Loan(string id = "loan-1", long outstanding = 10_000_000, int missed = 2) => new()
    {
        Id = id, Principal = 10_000_000, AnnualRatePercent = 10, TermSeasons = 5,
        SeasonsRemaining = 3, OutstandingBalance = outstanding, MissedPayments = missed,
    };

    private static Staff Staff(string id, long salary) => new()
    {
        Id = id, FirstName = id, LastName = "Eng", Role = StaffRole.TechnicalDirector,
        Skill = new Rating(70), Salary = salary,
    };

    // A one-board (alpha the player) carset where alpha carries a loan, two staff and an active bank whose
    // seizure/insolvency thresholds and points penalty are set.
    private static Carset Carset(IReadOnlyList<Loan>? loans = null, bool withBoard = true, bool withStaff = true)
    {
        var carset = CareerFixtures.Carset();
        var alpha = carset.Teams[0] with
        {
            Finances = new Finances { Balance = 500_000, Loans = loans ?? [Loan()] },
            Staff = withStaff ? [Staff("s-lo", 2_000_000), Staff("s-hi", 5_000_000)] : [],
        };
        var bank = new BankRules
        {
            BaseRatePercent = 6, MaxRiskPremiumPercent = 24, MaxLoanToRevenuePercent = 200,
            LatePenaltyPercent = 10, AssetSeizureAfterMisses = 2, InsolvencyAfterMisses = 4,
            InsolvencyPointsPenalty = 15,
        };
        var result = carset with { Teams = [alpha, carset.Teams[1]], Rules = carset.Rules with { Bank = bank } };
        return withBoard
            ? result with { PlayerTeamId = "alpha", Boards = [new TeamBoard { TeamId = "alpha" }] }
            : result;
    }

    private static MissedPayment Miss(int total, string loanId = "loan-1") =>
        new() { TeamId = "alpha", LoanId = loanId, MissedPaymentsTotal = total, AmountCapitalised = 1_000_000 };

    private static Team Alpha(Carset carset) => carset.Teams[0];

    private static TeamBoard AlphaBoard(EnforcementOutcome outcome) =>
        outcome.Carset.Boards.Single(b => b.TeamId == "alpha");

    [Fact]
    public void A_first_miss_is_a_warning_that_moves_board_pressure_only()
    {
        var outcome = BankEnforcement.Enforce(Carset(), [Miss(total: 1)]);

        var action = Assert.Single(outcome.Actions);
        Assert.Equal(EnforcementStep.Warning, action.Step);
        Assert.Equal(0, action.CashRaised);           // nothing seized yet
        Assert.Empty(outcome.PointsPenalties);        // no sporting penalty
        Assert.Equal(2, Alpha(outcome.Carset).Staff.Count); // staff intact

        var board = AlphaBoard(outcome);
        Assert.True(board.Metrics.BoardConfidence.Value < 50); // pressure moved
        Assert.True(board.Metrics.FinancialPressure.Value > 50);
    }

    [Fact]
    public void Repeated_misses_force_an_asset_sale_that_pays_down_the_debt()
    {
        var outcome = BankEnforcement.Enforce(Carset(), [Miss(total: 2)]);

        var action = Assert.Single(outcome.Actions);
        Assert.Equal(EnforcementStep.AssetSeizure, action.Step);
        Assert.Equal(5_000_000, action.CashRaised);              // the highest-paid staff member's value
        Assert.Equal("s-hi Eng", action.SeizedAsset);

        var alpha = Alpha(outcome.Carset);
        Assert.Single(alpha.Staff);                              // the top earner was released
        Assert.Equal("s-lo", alpha.Staff[0].Id);
        Assert.Equal(5_000_000, alpha.Finances.Loans[0].OutstandingBalance); // 10M − 5M proceeds
        Assert.Empty(outcome.PointsPenalties);                   // not terminal yet
    }

    [Fact]
    public void Sustained_default_docks_points_but_never_fires()
    {
        var carset = Carset();
        var before = AlphaBoard(new EnforcementOutcome { Carset = carset });

        var outcome = BankEnforcement.Enforce(carset, [Miss(total: 4)]);

        var action = Assert.Single(outcome.Actions);
        Assert.Equal(EnforcementStep.Insolvency, action.Step);
        Assert.Equal(15, action.PointsDocked);

        var penalty = Assert.Single(outcome.PointsPenalties);
        Assert.Equal("alpha", penalty.TeamId);
        Assert.Equal(15, penalty.PointsDeducted);

        // Heavy but survivable: confidence collapses, but firing risk is untouched — no game-over.
        var board = AlphaBoard(outcome);
        Assert.True(board.Metrics.BoardConfidence.Value < before.Metrics.BoardConfidence.Value);
        Assert.Equal(before.FiringRisk.Value, board.FiringRisk.Value);
    }

    [Fact]
    public void The_points_penalty_fires_once_on_crossing_not_every_terminal_season()
    {
        // Already past the terminal threshold (miss 5, never exactly 4 this season) → still liquidates and
        // pressures, but no fresh points penalty.
        var outcome = BankEnforcement.Enforce(Carset(), [Miss(total: 5)]);

        Assert.Equal(EnforcementStep.Insolvency, Assert.Single(outcome.Actions).Step);
        Assert.Empty(outcome.PointsPenalties);
    }

    [Fact]
    public void The_docked_points_apply_to_the_standings()
    {
        var outcome = BankEnforcement.Enforce(Carset(), [Miss(total: 4)]);
        var standings = new Standings
        {
            Drivers = [],
            Constructors =
            [
                new ConstructorStanding { Position = 1, TeamId = "alpha", Points = 40, Wins = 2 },
                new ConstructorStanding { Position = 2, TeamId = "bravo", Points = 30, Wins = 1 },
            ],
        };

        var docked = ConstructorPenalties.Apply(standings, outcome.PointsPenalties);

        Assert.Equal(25, docked.Constructors.Single(c => c.TeamId == "alpha").Points); // 40 − 15
    }

    [Fact]
    public void A_terminal_team_with_no_board_still_liquidates_and_docks()
    {
        var outcome = BankEnforcement.Enforce(Carset(withBoard: false), [Miss(total: 4)]);

        Assert.Equal(15, Assert.Single(outcome.PointsPenalties).PointsDeducted);
        Assert.Single(Alpha(outcome.Carset).Staff);      // still liquidated
        Assert.Empty(outcome.Carset.Boards);             // no board to pressure — handled gracefully
    }

    [Fact]
    public void A_seizure_with_no_staff_takes_no_cash_but_still_escalates()
    {
        var outcome = BankEnforcement.Enforce(Carset(withStaff: false), [Miss(total: 4)]);

        var action = Assert.Single(outcome.Actions);
        Assert.Equal(EnforcementStep.Insolvency, action.Step);
        Assert.Equal(0, action.CashRaised);              // nothing to seize
        Assert.Equal("", action.SeizedAsset);
        Assert.Equal(15, Assert.Single(outcome.PointsPenalties).PointsDeducted); // points still bite
    }

    [Fact]
    public void No_misses_leaves_the_carset_untouched()
    {
        var carset = Carset();

        var outcome = BankEnforcement.Enforce(carset, []);

        Assert.Same(carset, outcome.Carset);
        Assert.Empty(outcome.Actions);
        Assert.Empty(outcome.PointsPenalties);
    }

    [Fact]
    public void Enforcement_is_deterministic()
    {
        var carset = Carset();
        var first = BankEnforcement.Enforce(carset, [Miss(total: 4)]);
        var second = BankEnforcement.Enforce(carset, [Miss(total: 4)]);

        Assert.Equal(first.Actions, second.Actions);                        // EnforcementAction has value equality
        Assert.Equal(Alpha(first.Carset).Finances, Alpha(second.Carset).Finances); // debt paid down identically
        Assert.Equal(
            Alpha(first.Carset).Staff.Select(s => s.Id),
            Alpha(second.Carset).Staff.Select(s => s.Id));                  // same member released
    }
}
