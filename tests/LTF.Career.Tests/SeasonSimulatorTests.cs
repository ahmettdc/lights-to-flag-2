using System.Linq;
using Xunit;

namespace LTF.Career.Tests;

public class SeasonSimulatorTests
{
    [Fact]
    public void A_season_covers_every_round()
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 5);

        var result = SeasonSimulator.Run(carset, 7);

        Assert.Equal(5, result.Rounds.Count);
    }

    [Fact]
    public void A_season_is_deterministic()
    {
        var carset = CareerFixtures.SeasonCarset();

        var a = SeasonSimulator.Run(carset, 2024);
        var b = SeasonSimulator.Run(carset, 2024);

        Assert.Equal(Key(a), Key(b));
    }

    [Fact]
    public void A_faster_team_wins_both_titles()
    {
        // alpha (d1/d2, car 85) is clearly faster than bravo (d3/d4, car 70) over eight rounds.
        var carset = CareerFixtures.SeasonCarset(rounds: 8);

        var result = SeasonSimulator.Run(carset, 7);

        Assert.Equal("alpha", result.Standings.Constructors[0].TeamId);
        Assert.Contains(result.Standings.Drivers[0].DriverId, new[] { "d1", "d2" });
    }

    [Fact]
    public void Constructor_points_are_the_sum_of_the_teams_drivers()
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 4);

        var result = SeasonSimulator.Run(carset, 7);

        var alpha = result.Standings.Constructors.Single(c => c.TeamId == "alpha").Points;
        var d1 = result.Standings.Drivers.Single(d => d.DriverId == "d1").Points;
        var d2 = result.Standings.Drivers.Single(d => d.DriverId == "d2").Points;
        Assert.Equal(d1 + d2, alpha);
    }

    private static string Key(SeasonResult r) =>
        string.Join(";", r.Standings.Drivers.Select(d => $"{d.Position},{d.DriverId},{d.Points},{d.Wins}")) + "|" +
        string.Join(";", r.Standings.Constructors.Select(c => $"{c.Position},{c.TeamId},{c.Points},{c.Wins}"));
}
