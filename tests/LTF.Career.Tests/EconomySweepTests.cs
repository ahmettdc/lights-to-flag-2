using System.Linq;
using LTF.Domain;
using LTF.Domain.Common;
using LTF.Domain.Management;
using LTF.Domain.Racing;
using Xunit;

namespace LTF.Career.Tests;

public class EconomySweepTests
{
    // TV income exactly matches each team's staff bill, so — with no prize, sponsor or position-linked
    // money — every team nets zero every season and its balance never drifts. The cleanest possible
    // "self-balancing" case: whoever wins, the books stay put.
    private static Carset BalancedCarset()
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 6);
        var rules = carset.Rules with { Economy = new EconomyRules { TvIncome = 50_000_000 } };
        var teams = carset.Teams
            .Select(t => t with
            {
                Finances = new Finances { Balance = 100_000_000, CostCap = 0 },
                Staff = [Member(t.Id + "-s", 50_000_000)],
            })
            .ToList();
        return carset with { Rules = rules, Teams = teams };
    }

    // Staff cost dwarfs the income, so balances march downward into the red.
    private static Carset OverspendingCarset()
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 4);
        var rules = carset.Rules with { Economy = new EconomyRules { TvIncome = 20_000_000 } };
        var teams = carset.Teams
            .Select(t => t with
            {
                Finances = new Finances { Balance = 50_000_000, CostCap = 0 },
                Staff = [Member(t.Id + "-s", 100_000_000)],
            })
            .ToList();
        return carset with { Rules = rules, Teams = teams };
    }

    [Fact]
    public void A_balanced_economy_keeps_every_balance_steady_across_seasons()
    {
        var report = EconomySweep.Run(BalancedCarset(), seasons: 6, seed: 7);

        Assert.Equal(0, report.BankruptTeams);
        Assert.All(report.Teams, t => Assert.Equal(100_000_000L, t.FinalBalance));
        Assert.All(report.Teams, t => Assert.Equal(100_000_000L, t.MinBalance)); // never dipped
        Assert.All(report.Teams, t => Assert.Equal(100_000_000L, t.MaxBalance)); // never piled up
    }

    [Fact]
    public void A_team_that_cannot_cover_its_costs_goes_bankrupt()
    {
        var report = EconomySweep.Run(OverspendingCarset(), seasons: 5, seed: 7);

        Assert.True(report.BankruptTeams > 0);
        Assert.True(report.MinFinalBalance < 0);
        Assert.All(report.Teams, t => Assert.True(t.SeasonsInDebt > 0));
    }

    [Fact]
    public void An_economy_free_career_moves_no_money_over_a_sweep()
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 4); // no economy, default (zero) finances

        var report = EconomySweep.Run(carset, seasons: 3, seed: 7);

        Assert.Equal(0, report.BankruptTeams);
        Assert.All(report.Teams, t => Assert.Equal(0L, t.FinalBalance));
    }

    [Fact]
    public void The_sweep_is_deterministic()
    {
        var carset = BalancedCarset();

        Assert.Equal(Key(EconomySweep.Run(carset, 6, 7)), Key(EconomySweep.Run(carset, 6, 7)));
    }

    private static string Key(EconomySweepReport report) =>
        string.Join(";", report.Teams.Select(t =>
            $"{t.TeamId}:{t.FinalBalance},{t.MinBalance},{t.MaxBalance},{t.SeasonsInDebt},{t.CostCapPenalties}"));

    private static Staff Member(string id, long salary) => new()
    {
        Id = id, FirstName = "S", LastName = id, Role = StaffRole.RaceEngineer, Skill = new(70), Salary = salary,
    };
}
