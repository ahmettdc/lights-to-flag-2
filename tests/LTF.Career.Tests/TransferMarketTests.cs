using System.Collections.Generic;
using System.Linq;
using LTF.Domain;
using LTF.Domain.Racing;
using Xunit;

namespace LTF.Career.Tests;

public class TransferMarketTests
{
    private static DriverAttributes Flat(int v) => new()
    {
        Pace = new(v), Racecraft = new(v), Consistency = new(v),
        TyreManagement = new(v), WetWeather = new(v), Feedback = new(v),
    };

    private static Driver Driver(string id, int flat = 70) => new()
    {
        Id = id, FirstName = id, LastName = "D", Age = 25, Attributes = Flat(flat),
    };

    // alpha won the title (worst-first puts bravo before alpha, but only alpha has a vacancy).
    private static Standings TitleStandings() => new()
    {
        Drivers = [],
        Constructors =
        [
            new ConstructorStanding { Position = 1, TeamId = "alpha", Points = 100, Wins = 5 },
            new ConstructorStanding { Position = 2, TeamId = "bravo", Points = 50, Wins = 0 },
        ],
    };

    private static Dictionary<string, int> TwoEach() => new() { ["alpha"] = 2, ["bravo"] = 2 };

    // alpha has one open seat; the rest of the grid varies by which source can fill it.
    private static Carset VacancyCarset(IReadOnlyList<Driver> drivers, IReadOnlyList<Driver> reserves)
    {
        var carset = CareerFixtures.Carset();
        var alpha = carset.Teams[0] with { DriverIds = ["d1"] };
        var bravo = carset.Teams[1];
        return carset with { Teams = [alpha, bravo], Drivers = drivers, Reserves = reserves };
    }

    private static Team Alpha(Carset carset) => carset.Teams.Single(t => t.Id == "alpha");

    [Fact]
    public void A_free_agent_fills_the_vacant_seat()
    {
        // d2 is rostered but unseated → a free agent, signed before any reserve/rookie.
        var carset = VacancyCarset([Driver("d1"), Driver("d2"), Driver("d3"), Driver("d4")], []);

        var after = TransferMarket.Resolve(carset, TwoEach(), TitleStandings(), seed: 7);

        var alpha = Alpha(after);
        Assert.Equal(2, alpha.DriverIds.Count);
        Assert.Contains("d2", alpha.DriverIds);                          // the free agent took the seat
        Assert.Contains(after.Contracts, c => c.PartyId == "d2" && c.TeamId == "alpha");
    }

    [Fact]
    public void A_reserve_fills_the_seat_when_no_free_agent_is_available()
    {
        // No spare rostered driver, but a rookie waits in the reserves.
        var carset = VacancyCarset([Driver("d1"), Driver("d3"), Driver("d4")], [Driver("res1", flat: 65)]);

        var after = TransferMarket.Resolve(carset, TwoEach(), TitleStandings(), seed: 7);

        var alpha = Alpha(after);
        Assert.Equal(2, alpha.DriverIds.Count);
        Assert.Contains("res1", alpha.DriverIds);                       // promoted from the reserve pool
        Assert.DoesNotContain(after.Reserves, d => d.Id == "res1");     // and removed from it
        Assert.Contains(after.Drivers, d => d.Id == "res1");           // now on the roster
        Assert.Contains(after.Contracts, c => c.PartyId == "res1" && c.TeamId == "alpha");
    }

    [Fact]
    public void A_rookie_is_generated_when_the_pool_is_empty()
    {
        var carset = VacancyCarset([Driver("d1"), Driver("d3"), Driver("d4")], []);

        var after = TransferMarket.Resolve(carset, TwoEach(), TitleStandings(), seed: 7);

        var alpha = Alpha(after);
        Assert.Equal(2, alpha.DriverIds.Count);
        var newId = alpha.DriverIds.Single(id => id != "d1");
        Assert.StartsWith("regen-", newId);                            // invented a rookie
        Assert.Contains(after.Drivers, d => d.Id == newId);
        Assert.Contains(after.Contracts, c => c.PartyId == newId && c.TeamId == "alpha");
    }

    [Fact]
    public void A_full_grid_is_returned_untouched()
    {
        var carset = CareerFixtures.Carset(); // alpha and bravo both have two seats

        Assert.Same(carset, TransferMarket.Resolve(carset, TwoEach(), TitleStandings(), 7));
    }

    [Fact]
    public void The_transfer_market_is_deterministic()
    {
        var carset = VacancyCarset([Driver("d1"), Driver("d3"), Driver("d4")], []);

        Assert.Equal(
            Key(TransferMarket.Resolve(carset, TwoEach(), TitleStandings(), 7)),
            Key(TransferMarket.Resolve(carset, TwoEach(), TitleStandings(), 7)));
    }

    private static string Key(Carset carset) => string.Join("|", carset.Teams
        .Select(t => $"{t.Id}=[{string.Join(",", t.DriverIds)}]"));
}
