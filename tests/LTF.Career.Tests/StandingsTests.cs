using System.Linq;
using Xunit;

namespace LTF.Career.Tests;

public class StandingsTests
{
    [Fact]
    public void Points_accumulate_across_races()
    {
        var carset = CareerFixtures.Carset();
        var r1 = CareerFixtures.Race(("d1", 25), ("d2", 18), ("d3", 15), ("d4", 12));
        var r2 = CareerFixtures.Race(("d2", 25), ("d1", 18), ("d4", 15), ("d3", 12));

        var s = ChampionshipStandings.From(carset, [r1, r2]);

        Assert.Equal(43, s.Drivers.Single(d => d.DriverId == "d1").Points); // 25 + 18
        Assert.Equal(43, s.Drivers.Single(d => d.DriverId == "d2").Points); // 18 + 25
        Assert.Equal(27, s.Drivers.Single(d => d.DriverId == "d3").Points); // 15 + 12
    }

    [Fact]
    public void Constructor_points_are_the_sum_of_both_drivers()
    {
        var carset = CareerFixtures.Carset();
        var race = CareerFixtures.Race(("d1", 25), ("d3", 18), ("d2", 15), ("d4", 12));

        var s = ChampionshipStandings.From(carset, [race]);

        Assert.Equal(40, s.Constructors.Single(c => c.TeamId == "alpha").Points); // d1 25 + d2 15
        Assert.Equal(30, s.Constructors.Single(c => c.TeamId == "bravo").Points); // d3 18 + d4 12
        Assert.Equal("alpha", s.ConstructorsChampionId);
    }

    [Fact]
    public void The_leader_is_the_champion_and_positions_are_dense()
    {
        var carset = CareerFixtures.Carset();
        var race = CareerFixtures.Race(("d3", 25), ("d1", 18), ("d2", 15), ("d4", 12));

        var s = ChampionshipStandings.From(carset, [race]);

        Assert.Equal("d3", s.DriversChampionId);
        Assert.Equal(Enumerable.Range(1, 4), s.Drivers.Select(d => d.Position));
    }

    [Fact]
    public void Wins_and_podiums_are_counted()
    {
        var carset = CareerFixtures.Carset();
        var r1 = CareerFixtures.Race(("d1", 25), ("d2", 18), ("d3", 15), ("d4", 12));
        var r2 = CareerFixtures.Race(("d1", 25), ("d3", 18), ("d2", 15), ("d4", 12));

        var s = ChampionshipStandings.From(carset, [r1, r2]);

        var d1 = s.Drivers.Single(d => d.DriverId == "d1");
        Assert.Equal(2, d1.Wins);    // won both
        Assert.Equal(2, d1.Podiums); // top-3 both

        var d4 = s.Drivers.Single(d => d.DriverId == "d4");
        Assert.Equal(0, d4.Wins);
        Assert.Equal(0, d4.Podiums); // 4th both times
    }

    [Fact]
    public void Every_driver_and_team_appears_even_with_no_results()
    {
        var carset = CareerFixtures.Carset();

        var s = ChampionshipStandings.From(carset, []);

        Assert.Equal(4, s.Drivers.Count);
        Assert.Equal(2, s.Constructors.Count);
        Assert.All(s.Drivers, d => Assert.Equal(0, d.Points));
    }
}
