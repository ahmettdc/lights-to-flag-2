using System.Linq;
using LTF.Domain;
using LTF.Domain.Common;
using LTF.Domain.Management;
using LTF.Domain.Racing;
using Xunit;

namespace LTF.Career.Tests;

public class CostCapConsequencesTests
{
    // alpha carries a cost cap and a single staff member whose salary is its whole capped spend; the
    // series has no income, so the balance moves only by capped spend and any fine.
    private static Carset CapCarset(long staffSalary)
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 4);
        var rules = carset.Rules with
        {
            Economy = new EconomyRules { CostCapFinePercent = 100, CostCapPointsPerOverage = 10_000_000 },
        };
        var teams = carset.Teams
            .Select(t => t.Id == "alpha"
                ? t with { Finances = new Finances { Balance = 200_000_000, CostCap = 100_000_000 }, Staff = [Member("m1", staffSalary)] }
                : t with { Finances = new Finances { Balance = 200_000_000, CostCap = 100_000_000 } })
            .ToList();
        return carset with { Rules = rules, Teams = teams };
    }

    // --- f1: R&D spend folds into the cost cap ---

    [Fact]
    public void Folding_rnd_spend_into_the_cap_can_tip_a_team_over()
    {
        var carset = CapCarset(staffSalary: 90_000_000); // 90M of staff alone stays under the 100M cap
        var season = SeasonSimulator.Run(carset, 7);

        Assert.Empty(EconomyLedger.SettleSeason(carset, season).Penalties); // no R&D → within cap

        // 20M of R&D pushes capped spend to 110M > 100M cap → a breach.
        var rndSpend = new Dictionary<string, long> { ["alpha"] = 20_000_000 };
        var penalty = EconomyLedger.SettleSeason(carset, season, rndSpend).Penalties.Single();
        Assert.Equal("alpha", penalty.TeamId);
        Assert.Equal(10_000_000L, penalty.Overspend); // 110M − 100M
        Assert.Equal(10_000_000L, penalty.Fine);       // 100% of the overspend
    }

    [Fact]
    public void An_empty_rnd_spend_settles_identically_to_none()
    {
        var carset = CapCarset(staffSalary: 130_000_000); // breaches on staff alone
        var season = SeasonSimulator.Run(carset, 7);

        var none = EconomyLedger.SettleSeason(carset, season);
        var empty = EconomyLedger.SettleSeason(carset, season, new Dictionary<string, long>());

        Assert.Equal(none.Penalties.Single().Overspend, empty.Penalties.Single().Overspend);
        Assert.Equal(
            none.Carset.Teams.Single(t => t.Id == "alpha").Finances.Balance,
            empty.Carset.Teams.Single(t => t.Id == "alpha").Finances.Balance);
    }

    [Fact]
    public void Rnd_folded_settlement_is_deterministic()
    {
        var carset = CapCarset(staffSalary: 90_000_000);
        var season = SeasonSimulator.Run(carset, 7);
        var rndSpend = new Dictionary<string, long> { ["alpha"] = 20_000_000 };

        var a = EconomyLedger.SettleSeason(carset, season, rndSpend).Penalties.Single();
        var b = EconomyLedger.SettleSeason(carset, season, rndSpend).Penalties.Single();
        Assert.Equal(a.Overspend, b.Overspend);
        Assert.Equal(a.Fine, b.Fine);
    }

    [Fact]
    public void Rnd_spend_counts_against_the_cap_but_is_not_charged_to_the_balance()
    {
        var carset = CapCarset(staffSalary: 90_000_000); // 90M of staff alone stays under the 100M cap
        var season = SeasonSimulator.Run(carset, 7);
        var rndSpend = new Dictionary<string, long> { ["alpha"] = 20_000_000 };

        var withoutRnd = EconomyLedger.SettleSeason(carset, season)
            .Carset.Teams.Single(t => t.Id == "alpha").Finances.Balance;
        var settlement = EconomyLedger.SettleSeason(carset, season, rndSpend);
        var withRnd = settlement.Carset.Teams.Single(t => t.Id == "alpha").Finances.Balance;

        // Folding 20M R&D tips alpha over the cap (90M + 20M > 100M); the only change to the balance is the
        // resulting fine — the R&D itself is not re-charged, since the research ledger already paid for it.
        Assert.Equal(withoutRnd - settlement.Penalties.Single().Fine, withRnd);
    }

    // --- f2: applying the deferred points to the constructors' table ---

    [Fact]
    public void Applying_a_points_deduction_demotes_a_constructor()
    {
        var standings = new Standings
        {
            Drivers = [],
            Constructors =
            [
                new ConstructorStanding { Position = 1, TeamId = "alpha", Points = 100, Wins = 5 },
                new ConstructorStanding { Position = 2, TeamId = "bravo", Points = 80, Wins = 2 },
            ],
        };
        var penalties = new[] { new CostCapPenalty { TeamId = "alpha", PointsDeducted = 40 } };

        var adjusted = ConstructorPenalties.Apply(standings, penalties);

        // alpha 100 − 40 = 60 now trails bravo's 80, so bravo leads.
        Assert.Equal("bravo", adjusted.Constructors[0].TeamId);
        Assert.Equal(1, adjusted.Constructors[0].Position);
        var alpha = adjusted.Constructors.Single(c => c.TeamId == "alpha");
        Assert.Equal(60, alpha.Points);
        Assert.Equal(2, alpha.Position);
    }

    [Fact]
    public void Applying_no_penalties_returns_the_same_standings()
    {
        var standings = Table();
        Assert.Same(standings, ConstructorPenalties.Apply(standings, []));
    }

    [Fact]
    public void Applying_only_zero_point_penalties_returns_the_same_standings()
    {
        var standings = Table();
        var penalties = new[] { new CostCapPenalty { TeamId = "alpha", PointsDeducted = 0, Fine = 5_000_000 } };
        Assert.Same(standings, ConstructorPenalties.Apply(standings, penalties));
    }

    private static Standings Table() => new()
    {
        Drivers = [],
        Constructors = [new ConstructorStanding { Position = 1, TeamId = "alpha", Points = 100, Wins = 5 }],
    };

    private static Staff Member(string id, long salary) => new()
    {
        Id = id, FirstName = "S", LastName = id, Role = StaffRole.RaceEngineer, Skill = new(70), Salary = salary,
    };
}
