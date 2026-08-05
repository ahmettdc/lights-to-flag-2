using System.Linq;
using LTF.Domain;
using LTF.Domain.Common;
using LTF.Domain.Management;
using LTF.Domain.Racing;
using LTF.Simulation.Racing;
using Xunit;

namespace LTF.Career.Tests;

public class EconomyExpenseTests
{
    // A season carset with prize/TV economy and per-team finances, but no sponsors, staff, contracts
    // or operating/crash cost — a clean baseline to add one expense at a time and measure it.
    private static Carset EconomyCarset(int rounds = 8)
    {
        var carset = CareerFixtures.SeasonCarset(rounds: rounds);
        var rules = carset.Rules with { Economy = new EconomyRules { PrizeMoney = [100_000_000, 60_000_000], TvIncome = 20_000_000 } };
        var teams = carset.Teams
            .Select(t => t with { Finances = new Finances { Balance = 100_000_000, CostCap = 135_000_000 } })
            .ToList();
        return carset with { Rules = rules, Teams = teams };
    }

    private static long Balance(Carset carset, string teamId) =>
        carset.Teams.Single(t => t.Id == teamId).Finances.Balance;

    private static long Balance(SeasonSettlement settlement, string teamId) =>
        Balance(settlement.Carset, teamId);

    [Fact]
    public void Staff_salaries_reduce_the_balance_by_their_total()
    {
        var carset = EconomyCarset();
        var withStaff = carset with
        {
            Teams = carset.Teams
                .Select(t => t.Id == "alpha" ? t with { Staff = [Member("m1", 4_000_000), Member("m2", 1_500_000)] } : t)
                .ToList(),
        };
        var season = SeasonSimulator.Run(carset, 7); // economy/staff don't affect the sim

        var without = Balance(EconomyLedger.SettleSeason(carset, season), "alpha");
        var with = Balance(EconomyLedger.SettleSeason(withStaff, season), "alpha");

        Assert.Equal(without - 5_500_000L, with); // 4.0M + 1.5M
    }

    [Fact]
    public void Driver_salaries_reduce_the_balance()
    {
        var carset = EconomyCarset();
        var withContract = carset with { Contracts = [DriverContract("d1", "alpha", 3_000_000)] };
        var season = SeasonSimulator.Run(carset, 7);

        var without = Balance(EconomyLedger.SettleSeason(carset, season), "alpha");
        var with = Balance(EconomyLedger.SettleSeason(withContract, season), "alpha");

        Assert.Equal(without - 3_000_000L, with);
    }

    [Fact]
    public void Operating_cost_scales_with_the_number_of_races()
    {
        var carset = EconomyCarset(rounds: 5);
        var withOps = carset with
        {
            Rules = carset.Rules with { Economy = carset.Rules.Economy with { OperatingCostPerRace = 2_000_000 } },
        };
        var season = SeasonSimulator.Run(carset, 7);

        var without = Balance(EconomyLedger.SettleSeason(carset, season), "alpha");
        var with = Balance(EconomyLedger.SettleSeason(withOps, season), "alpha");

        Assert.Equal(without - 2_000_000L * 5, with); // 5 rounds
    }

    [Fact]
    public void Crash_cost_is_booked_per_car_involved()
    {
        var baseCarset = EconomyCarset();
        var carset = baseCarset with
        {
            Rules = baseCarset.Rules with { Economy = new EconomyRules { CrashCostPerIncident = 1_000_000 } },
        };

        // One hand-built round: alpha's d1 collides (as primary), d2 collides (as the other car) → 2
        // alpha incidents; d3 and d4 (bravo) are each involved once too → 2 bravo incidents.
        var rounds = new[]
        {
            CareerFixtures.Race(("d1", 25), ("d2", 18), ("d3", 15), ("d4", 12))
                with { Events = [Collision("d1", "d3"), Collision("d4", "d2")] },
        };
        var season = new SeasonResult
        {
            Standings = ChampionshipStandings.From(carset, rounds),
            Rounds = rounds,
            PoleSitters = [],
        };

        var settled = EconomyLedger.SettleSeason(carset, season);

        // No income (crash-only economy), so each team lost exactly 2 × 1M in repairs.
        Assert.Equal(100_000_000L - 2_000_000L, Balance(settled, "alpha"));
        Assert.Equal(100_000_000L - 2_000_000L, Balance(settled, "bravo"));
    }

    [Fact]
    public void A_team_that_outspends_its_income_goes_into_the_red()
    {
        var carset = EconomyCarset();
        var broke = carset with
        {
            Teams = carset.Teams
                .Select(t => t.Id == "alpha"
                    ? t with { Finances = t.Finances with { Balance = 0 }, Staff = [Member("m", 500_000_000)] }
                    : t)
                .ToList(),
        };
        var season = SeasonSimulator.Run(carset, 7);

        Assert.True(Balance(EconomyLedger.SettleSeason(broke, season), "alpha") < 0);
    }

    private static Staff Member(string id, long salary) => new()
    {
        Id = id, FirstName = "S", LastName = id, Role = StaffRole.RaceEngineer, Skill = new(70), Salary = salary,
    };

    private static Contract DriverContract(string driverId, string teamId, long salary) => new()
    {
        Kind = ContractKind.Driver, PartyId = driverId, TeamId = teamId, SalaryPerSeason = salary, SeasonsRemaining = 2,
    };

    private static RaceEvent Collision(string a, string b) => new()
    {
        Kind = RaceEventKind.Collision, Lap = 5, CompetitorId = a, OtherCompetitorId = b, Description = "contact",
    };
}
