using System.Linq;
using LTF.Domain;
using LTF.Domain.Racing;
using LTF.Simulation.Racing;
using Xunit;

namespace LTF.Career.Tests;

public class CareerRolloverTests
{
    // Two rounds: d1 wins the first, d2 the second; d3 is always third, d4 always fourth.
    // Fastest laps go to d2 then d3; poles to d1 then d3.
    private static (Carset Carset, SeasonResult Season) TwoRoundSeason()
    {
        var carset = CareerFixtures.Carset();
        var r1 = CareerFixtures.Race(("d1", 25), ("d2", 18), ("d3", 15), ("d4", 12))
            with { FastestLapCompetitorId = "d2" };
        var r2 = CareerFixtures.Race(("d2", 25), ("d1", 18), ("d3", 15), ("d4", 12))
            with { FastestLapCompetitorId = "d3" };
        var rounds = new[] { r1, r2 };
        var standings = ChampionshipStandings.From(carset, rounds);
        var season = new SeasonResult
        {
            Standings = standings,
            Rounds = rounds,
            PoleSitters = ["d1", "d3"],
        };
        return (carset, season);
    }

    private static DriverCareer CareerOf(Carset carset, string id) =>
        carset.Drivers.Single(d => d.Id == id).Career;

    [Fact]
    public void A_season_accumulates_onto_each_drivers_record()
    {
        var (carset, season) = TwoRoundSeason();

        var after = CareerRollover.Apply(carset, season);

        var d1 = CareerOf(after, "d1");
        Assert.Equal(2, d1.Races);
        Assert.Equal(1, d1.Wins);       // won round 1
        Assert.Equal(2, d1.Podiums);    // 1st then 2nd
        Assert.Equal(1, d1.Poles);      // pole in round 1
        Assert.Equal(0, d1.FastestLaps);
        Assert.Equal(43.0, d1.Points);    // 25 + 18

        var d3 = CareerOf(after, "d3");
        Assert.Equal(2, d3.Races);
        Assert.Equal(0, d3.Wins);
        Assert.Equal(2, d3.Podiums);    // third both times
        Assert.Equal(1, d3.Poles);      // pole in round 2
        Assert.Equal(1, d3.FastestLaps); // fastest lap in round 2
        Assert.Equal(30.0, d3.Points);

        var d4 = CareerOf(after, "d4");
        Assert.Equal(0, d4.Podiums);    // fourth both times — no podium
        Assert.Equal(24.0, d4.Points);
    }

    [Fact]
    public void The_champions_gain_a_title_and_team_wins_sum_their_drivers()
    {
        var (carset, season) = TwoRoundSeason();

        var after = CareerRollover.Apply(carset, season);

        // d1 and d2 tie on 43 points and one win each; the id tiebreak crowns d1.
        Assert.Equal("d1", season.DriversChampionId);
        Assert.Equal(1, CareerOf(after, "d1").Championships);
        Assert.Equal(0, CareerOf(after, "d2").Championships);

        var alpha = after.Teams.Single(t => t.Id == "alpha");
        var bravo = after.Teams.Single(t => t.Id == "bravo");
        Assert.Equal(1, alpha.ChampionshipsWon);   // alpha (d1/d2) took the constructors' title
        Assert.Equal(2, alpha.RaceWins);           // d1 and d2 each won a round
        Assert.Equal(0, bravo.ChampionshipsWon);
        Assert.Equal(0, bravo.RaceWins);
    }

    [Fact]
    public void Chaining_seasons_adds_to_the_existing_record()
    {
        var (carset, season) = TwoRoundSeason();

        var afterOne = CareerRollover.Apply(carset, season);
        var afterTwo = CareerRollover.Apply(afterOne, season);

        var d1 = CareerOf(afterTwo, "d1");
        Assert.Equal(4, d1.Races);          // two seasons of two races
        Assert.Equal(2, d1.Wins);
        Assert.Equal(2, d1.Championships);
        Assert.Equal(86.0, d1.Points);
        Assert.Equal(4, afterTwo.Teams.Single(t => t.Id == "alpha").RaceWins); // two wins per season
    }

    [Fact]
    public void A_simulated_season_rolls_poles_fastest_laps_and_points_up()
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 6);
        var season = SeasonSimulator.Run(carset, 7);

        var after = CareerRollover.Apply(carset, season);

        // Every one of the four drivers started all six rounds.
        Assert.All(after.Drivers, d => Assert.Equal(6, d.Career.Races));

        // One pole per round; one fastest lap per round that recorded one.
        Assert.Equal(6, after.Drivers.Sum(d => d.Career.Poles));
        Assert.Equal(
            season.Rounds.Count(r => r.FastestLapCompetitorId is not null),
            after.Drivers.Sum(d => d.Career.FastestLaps));

        // Points match the championship table, since every record started empty.
        foreach (var standing in season.Standings.Drivers)
        {
            Assert.Equal((double)standing.Points, CareerOf(after, standing.DriverId).Points);
        }

        // The champion is the sole driver with a title.
        Assert.Equal(1, after.Drivers.Count(d => d.Career.Championships == 1));
        Assert.Equal(
            season.DriversChampionId,
            after.Drivers.Single(d => d.Career.Championships == 1).Id);
    }

    [Fact]
    public void The_rollover_is_deterministic()
    {
        var (carset, season) = TwoRoundSeason();

        Assert.Equal(Key(CareerRollover.Apply(carset, season)), Key(CareerRollover.Apply(carset, season)));
    }

    private static string Key(Carset carset) =>
        string.Join(";", carset.Drivers.Select(d =>
            $"{d.Id}:{d.Career.Races},{d.Career.Wins},{d.Career.Podiums},{d.Career.Poles}," +
            $"{d.Career.FastestLaps},{d.Career.Championships},{d.Career.Points}")) + "|" +
        string.Join(";", carset.Teams.Select(t => $"{t.Id}:{t.ChampionshipsWon},{t.RaceWins}"));
}
