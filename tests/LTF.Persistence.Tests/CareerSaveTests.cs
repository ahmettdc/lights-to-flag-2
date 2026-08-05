using System.Linq;
using LTF.Career;
using LTF.Domain;
using LTF.Domain.Common;
using LTF.Domain.Racing;
using Xunit;

namespace LTF.Persistence.Tests;

public class CareerSaveTests
{
    // A career snapshot with a non-integer points total, to exercise number round-tripping.
    private static CareerState Sample() => new()
    {
        Seed = 2025,
        Date = new DateOnly(2025, 7, 4),
        Drivers =
        [
            new DriverCareerRecord
            {
                DriverId = "d1",
                Career = new DriverCareer
                {
                    Races = 40, Wins = 6, Podiums = 15, Poles = 4,
                    FastestLaps = 3, Championships = 1, Points = 512.5,
                },
            },
            new DriverCareerRecord
            {
                DriverId = "d2",
                Career = new DriverCareer { Races = 40, Podiums = 2, FastestLaps = 1, Points = 88 },
            },
        ],
        Teams =
        [
            new TeamHistoryRecord { TeamId = "alpha", ChampionshipsWon = 2, RaceWins = 18 },
            new TeamHistoryRecord { TeamId = "bravo", RaceWins = 1 },
        ],
    };

    [Fact]
    public void A_round_trip_preserves_every_record()
    {
        var state = Sample();

        var loaded = CareerStore.Deserialize(CareerStore.Serialize(state));

        Assert.Equal(state.Seed, loaded.Seed);
        Assert.Equal(state.Date, loaded.Date);
        Assert.Equal(state.Drivers.Count, loaded.Drivers.Count);
        for (var i = 0; i < state.Drivers.Count; i++)
        {
            Assert.Equal(state.Drivers[i], loaded.Drivers[i]); // record value equality (id + career)
        }

        Assert.Equal(state.Teams.Count, loaded.Teams.Count);
        for (var i = 0; i < state.Teams.Count; i++)
        {
            Assert.Equal(state.Teams[i], loaded.Teams[i]);
        }
    }

    [Fact]
    public void Serialization_is_stable_across_a_round_trip()
    {
        var state = Sample();

        var once = CareerStore.Serialize(state);
        var twice = CareerStore.Serialize(CareerStore.Deserialize(once));

        Assert.Equal(once, twice); // byte-for-byte identical
    }

    [Fact]
    public void A_capture_survives_a_save_and_load_through_disk()
    {
        var state = CareerState.Capture(MiniCarset(), new DateOnly(2025, 9, 1), 7);
        var dir = Directory.CreateTempSubdirectory("ltf-career-tests");
        try
        {
            var path = Path.Combine(dir.FullName, "save.json");

            CareerStore.Save(state, path);
            var loaded = CareerStore.Load(path);

            Assert.Equal(CareerStore.Serialize(state), CareerStore.Serialize(loaded));
            Assert.Equal(state.Date, loaded.Date);
            Assert.Equal(state.Seed, loaded.Seed);
            Assert.Equal(state.Drivers[0].Career, loaded.Drivers[0].Career);
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    [Fact]
    public void Capture_then_restore_returns_the_records_to_a_fresh_carset()
    {
        var played = MiniCarset(); // a carset mid-career

        var state = CareerState.Capture(played, new DateOnly(2025, 9, 1), 7);

        // A fresh copy with the driver and team records reset to debut.
        var fresh = played with
        {
            Drivers = [played.Drivers[0] with { Career = DriverCareer.None }, played.Drivers[1]],
            Teams = [played.Teams[0] with { ChampionshipsWon = 0, RaceWins = 0 }],
        };
        Assert.Equal(0, fresh.Drivers[0].Career.Wins); // sanity: really reset

        var resumed = state.RestoreInto(fresh);

        Assert.Equal(6, resumed.Drivers[0].Career.Wins);
        Assert.Equal(512.0, resumed.Drivers[0].Career.Points);
        Assert.Equal(2, resumed.Teams[0].ChampionshipsWon);
        Assert.Equal(18, resumed.Teams[0].RaceWins);
    }

    [Fact]
    public void Restore_leaves_records_the_snapshot_does_not_mention_untouched()
    {
        var carset = MiniCarset(); // d1 evolved, d2 on debut, one team

        var partial = new CareerState
        {
            Seed = 1,
            Date = new DateOnly(2025, 1, 1),
            Drivers = [new DriverCareerRecord { DriverId = "d1", Career = new DriverCareer { Races = 99 } }],
            Teams = [],
        };

        var restored = partial.RestoreInto(carset);

        Assert.Equal(99, restored.Drivers.Single(d => d.Id == "d1").Career.Races);       // replaced
        Assert.Equal(DriverCareer.None, restored.Drivers.Single(d => d.Id == "d2").Career); // untouched
        Assert.Equal(carset.Teams[0].RaceWins, restored.Teams[0].RaceWins);              // no team lines → untouched
    }

    private static Carset MiniCarset() => new()
    {
        Id = "mini",
        Name = "Mini",
        Rules = new RulesSet { SeriesName = "S", Points = new PointsScheme { RacePoints = [25, 18] } },
        Teams =
        [
            new Team { Id = "alpha", Name = "Alpha", Car = Flat(70), DriverIds = ["d1"], ChampionshipsWon = 2, RaceWins = 18 },
        ],
        Drivers =
        [
            new Driver
            {
                Id = "d1", FirstName = "Ada", LastName = "One", Age = 24, Attributes = Attrs(),
                Career = new DriverCareer
                {
                    Races = 40, Wins = 6, Podiums = 15, Poles = 4,
                    FastestLaps = 3, Championships = 1, Points = 512,
                },
            },
            new Driver { Id = "d2", FirstName = "Ben", LastName = "Two", Age = 22, Attributes = Attrs() },
        ],
        Circuits = [],
        Calendar = [],
    };

    private static Car Flat(int v) => new()
    {
        Aerodynamics = new(v), Chassis = new(v), PowerUnit = new(v), TyreGentleness = new(v), Reliability = new(v),
    };

    private static DriverAttributes Attrs() => new()
    {
        Pace = new(70), Racecraft = new(70), Consistency = new(70),
        TyreManagement = new(70), WetWeather = new(70), Feedback = new(70),
    };
}
