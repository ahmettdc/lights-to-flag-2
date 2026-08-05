using System.Linq;
using LTF.Domain;
using LTF.Domain.Common;
using LTF.Domain.Management;
using LTF.Domain.Racing;
using Xunit;

namespace LTF.Career.Tests;

public class BudgetCapTests
{
    // A carset carrying cost-cap penalty coefficients and a per-team cap, but no income at all (empty
    // prize/TV/sponsor) — so the balance only moves by capped spend and the fine, making the arithmetic
    // exact regardless of the race result.
    private static Carset CapCarset(long costCap, int finePercent, long pointsPerOverage)
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 4);
        var rules = carset.Rules with
        {
            Economy = new EconomyRules
            {
                CostCapFinePercent = finePercent,
                CostCapPointsPerOverage = pointsPerOverage,
            },
        };
        var teams = carset.Teams
            .Select(t => t with { Finances = new Finances { Balance = 200_000_000, CostCap = costCap } })
            .ToList();
        return carset with { Rules = rules, Teams = teams };
    }

    // Give alpha a single staff member whose salary is its whole capped spend.
    private static Carset WithAlphaStaff(Carset carset, long salary) => carset with
    {
        Teams = carset.Teams
            .Select(t => t.Id == "alpha" ? t with { Staff = [Member("m1", salary)] } : t)
            .ToList(),
    };

    [Fact]
    public void Overspending_the_capped_budget_raises_a_penalty()
    {
        var carset = WithAlphaStaff(CapCarset(costCap: 100_000_000, finePercent: 100, pointsPerOverage: 10_000_000), 130_000_000);
        var season = SeasonSimulator.Run(carset, 7);

        // Only alpha breaches (bravo spends nothing), so there is exactly one penalty.
        var penalty = EconomyLedger.SettleSeason(carset, season).Penalties.Single();

        Assert.Equal("alpha", penalty.TeamId);
        Assert.Equal(30_000_000L, penalty.Overspend);   // 130M spend − 100M cap
        Assert.Equal(30_000_000L, penalty.Fine);         // 100% of the overspend
        Assert.Equal(3, penalty.PointsDeducted);         // 30M / 10M per point
        Assert.True(penalty.AeroTestRestricted);
    }

    [Fact]
    public void The_fine_is_deducted_from_the_balance()
    {
        var carset = WithAlphaStaff(CapCarset(100_000_000, finePercent: 100, pointsPerOverage: 10_000_000), 130_000_000);
        var season = SeasonSimulator.Run(carset, 7);

        var alpha = EconomyLedger.SettleSeason(carset, season).Carset.Teams.Single(t => t.Id == "alpha");

        // 200M start − 130M staff − 30M fine, with no income.
        Assert.Equal(40_000_000L, alpha.Finances.Balance);
    }

    [Fact]
    public void Staying_within_the_cap_raises_no_penalty()
    {
        // 90M of capped spend stays under the 100M cap.
        var carset = WithAlphaStaff(CapCarset(100_000_000, 100, 10_000_000), 90_000_000);
        var season = SeasonSimulator.Run(carset, 7);

        Assert.Empty(EconomyLedger.SettleSeason(carset, season).Penalties);
    }

    [Fact]
    public void A_series_without_a_cost_cap_raises_no_penalty()
    {
        // Huge spend, but a zero cap means the series runs no cap at all.
        var carset = WithAlphaStaff(CapCarset(costCap: 0, finePercent: 100, pointsPerOverage: 10_000_000), 500_000_000);
        var season = SeasonSimulator.Run(carset, 7);

        Assert.Empty(EconomyLedger.SettleSeason(carset, season).Penalties);
    }

    [Fact]
    public void A_breach_still_restricts_aero_but_deducts_no_points_when_points_are_disabled()
    {
        var carset = WithAlphaStaff(CapCarset(100_000_000, finePercent: 100, pointsPerOverage: 0), 130_000_000);
        var season = SeasonSimulator.Run(carset, 7);

        var penalty = EconomyLedger.SettleSeason(carset, season).Penalties.Single();

        Assert.Equal(30_000_000L, penalty.Fine);
        Assert.Equal(0, penalty.PointsDeducted);
        Assert.True(penalty.AeroTestRestricted);
    }

    [Fact]
    public void Penalty_settlement_is_deterministic()
    {
        var carset = WithAlphaStaff(CapCarset(100_000_000, 100, 10_000_000), 130_000_000);
        var season = SeasonSimulator.Run(carset, 7);

        Assert.Equal(Key(EconomyLedger.SettleSeason(carset, season)), Key(EconomyLedger.SettleSeason(carset, season)));
    }

    private static string Key(SeasonSettlement settlement) =>
        string.Join(";", settlement.Penalties.Select(p =>
            $"{p.TeamId}:{p.Overspend},{p.Fine},{p.PointsDeducted},{p.AeroTestRestricted}"));

    private static Staff Member(string id, long salary) => new()
    {
        Id = id, FirstName = "S", LastName = id, Role = StaffRole.RaceEngineer, Skill = new(70), Salary = salary,
    };
}
