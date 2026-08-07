using System;
using System.Linq;
using LTF.Domain;
using LTF.Domain.Racing;
using Xunit;

namespace LTF.Career.Tests;

public class SeasonArchiveTests
{
    // A two-round, two-circuit carset raced in 2025 (alpha = d1/d2, bravo = d3/d4).
    private static Carset TwoCircuitCarset() => CareerFixtures.Carset() with
    {
        Circuits =
        [
            new Circuit { Id = "aaa", Name = "A", Laps = 50, LapDistanceKm = 5.0, BaseLapTimeSeconds = 80.0 },
            new Circuit { Id = "bbb", Name = "B", Laps = 50, LapDistanceKm = 5.0, BaseLapTimeSeconds = 90.0 },
        ],
        Calendar =
        [
            new CalendarRound { Round = 1, CircuitId = "aaa", Date = new DateOnly(2025, 3, 16) },
            new CalendarRound { Round = 2, CircuitId = "bbb", Date = new DateOnly(2025, 4, 20) },
        ],
    };

    // d1 wins round 1 (fastest lap 79.5 at aaa), d2 wins round 2 (fastest lap 88.0 at bbb).
    private static SeasonResult Season(Carset carset)
    {
        var r1 = CareerFixtures.Race(("d1", 25), ("d2", 18), ("d3", 15), ("d4", 12))
            with { FastestLapCompetitorId = "d1", FastestLapTime = 79.5 };
        var r2 = CareerFixtures.Race(("d2", 25), ("d1", 18), ("d3", 15), ("d4", 12))
            with { FastestLapCompetitorId = "d2", FastestLapTime = 88.0 };
        var rounds = new[] { r1, r2 };
        return new SeasonResult
        {
            Standings = ChampionshipStandings.From(carset, rounds),
            Rounds = rounds,
            PoleSitters = ["d1", "d2"],
        };
    }

    [Fact]
    public void A_season_is_archived_with_its_year_champions_and_final_tables()
    {
        var carset = TwoCircuitCarset();
        var season = Season(carset);

        var after = SeasonArchive.Append(carset, carset, season);

        var record = Assert.Single(after.SeasonHistory);
        Assert.Equal(2025, record.Year);                                  // the first round's year
        Assert.Equal(season.DriversChampionId, record.DriversChampionId);
        Assert.Equal(season.ConstructorsChampionId, record.ConstructorsChampionId);
        Assert.Equal(carset.Drivers.Count, record.Drivers.Count);          // every driver line
        Assert.Equal(carset.Teams.Count, record.Constructors.Count);
        Assert.Equal(record.DriversChampionId, record.Drivers[0].DriverId); // best-first
        Assert.Equal(1, record.Drivers[0].Position);
    }

    [Fact]
    public void The_season_record_carries_the_final_standings_lines()
    {
        var carset = TwoCircuitCarset();
        var season = Season(carset);

        var record = Assert.Single(SeasonArchive.Append(carset, carset, season).SeasonHistory);

        // Each archived driver line mirrors the championship table it was built from.
        foreach (var standing in season.Standings.Drivers)
        {
            var line = record.Drivers.Single(d => d.DriverId == standing.DriverId);
            Assert.Equal(standing.Position, line.Position);
            Assert.Equal(standing.Points, line.Points);
            Assert.Equal(standing.Wins, line.Wins);
            Assert.Equal(standing.Podiums, line.Podiums);
        }
    }

    [Fact]
    public void Track_records_capture_the_fastest_lap_at_each_circuit()
    {
        var carset = TwoCircuitCarset();

        var after = SeasonArchive.Append(carset, carset, Season(carset));

        Assert.Equal(2, after.TrackRecords.Count);
        var aaa = after.TrackRecords.Single(t => t.CircuitId == "aaa");
        Assert.Equal(79.5, aaa.BestLapSeconds);
        Assert.Equal("d1", aaa.DriverId);
        Assert.Equal(2025, aaa.Year);
    }

    [Fact]
    public void Chaining_seasons_appends_history_and_keeps_only_the_faster_lap()
    {
        var carset = TwoCircuitCarset();
        var afterOne = SeasonArchive.Append(carset, carset, Season(carset)); // aaa 79.5, bbb 88.0

        // Season two: quicker at aaa (78.0), slower at bbb (95.0).
        var s2r1 = CareerFixtures.Race(("d1", 25), ("d2", 18), ("d3", 15), ("d4", 12))
            with { FastestLapCompetitorId = "d3", FastestLapTime = 78.0 };
        var s2r2 = CareerFixtures.Race(("d2", 25), ("d1", 18), ("d3", 15), ("d4", 12))
            with { FastestLapCompetitorId = "d4", FastestLapTime = 95.0 };
        var rounds2 = new[] { s2r1, s2r2 };
        var season2 = new SeasonResult
        {
            Standings = ChampionshipStandings.From(carset, rounds2), Rounds = rounds2, PoleSitters = ["d3", "d4"],
        };

        var afterTwo = SeasonArchive.Append(afterOne, carset, season2);

        Assert.Equal(2, afterTwo.SeasonHistory.Count);   // accumulates, never cleared
        Assert.Equal(2, afterTwo.TrackRecords.Count);    // still two circuits, no duplicate rows

        var aaa = afterTwo.TrackRecords.Single(t => t.CircuitId == "aaa");
        Assert.Equal(78.0, aaa.BestLapSeconds);          // improved
        Assert.Equal("d3", aaa.DriverId);

        var bbb = afterTwo.TrackRecords.Single(t => t.CircuitId == "bbb");
        Assert.Equal(88.0, bbb.BestLapSeconds);          // NOT replaced by the slower 95.0
        Assert.Equal("d2", bbb.DriverId);
    }

    [Fact]
    public void A_round_without_a_fastest_lap_sets_no_track_record()
    {
        var carset = TwoCircuitCarset();
        var r1 = CareerFixtures.Race(("d1", 25), ("d2", 18), ("d3", 15), ("d4", 12)); // no fastest lap (0.0)
        var r2 = CareerFixtures.Race(("d2", 25), ("d1", 18), ("d3", 15), ("d4", 12))
            with { FastestLapCompetitorId = "d2", FastestLapTime = 88.0 };
        var rounds = new[] { r1, r2 };
        var season = new SeasonResult
        {
            Standings = ChampionshipStandings.From(carset, rounds), Rounds = rounds, PoleSitters = [null, "d2"],
        };

        var after = SeasonArchive.Append(carset, carset, season);

        var record = Assert.Single(after.TrackRecords);  // only bbb; aaa had no valid fastest lap
        Assert.Equal("bbb", record.CircuitId);
    }

    [Fact]
    public void Archiving_is_deterministic()
    {
        var carset = TwoCircuitCarset();
        var season = Season(carset);

        Assert.Equal(Key(SeasonArchive.Append(carset, carset, season)), Key(SeasonArchive.Append(carset, carset, season)));
    }

    private static string Key(Carset carset) =>
        string.Join(";", carset.SeasonHistory.Select(s =>
            $"{s.Year}:{s.DriversChampionId}:{s.ConstructorsChampionId}:" +
            string.Join(",", s.Drivers.Select(d => $"{d.Position}-{d.DriverId}-{d.Points}")))) + "|" +
        string.Join(";", carset.TrackRecords.Select(t => $"{t.CircuitId}-{t.BestLapSeconds}-{t.DriverId}-{t.Year}"));
}
