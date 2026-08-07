using System.Linq;
using LTF.Career;
using LTF.Domain;
using LTF.Domain.Racing;
using Xunit;

namespace LTF.Persistence.Tests;

/// <summary>
/// Season-archive persistence (M24): the season history and per-circuit track records accumulate on the carset and
/// are never reconstructed, so they are captured and restored through a save. Empty by default → a career that has
/// completed no season is byte-identical. Value-typed → round-trips byte-stably.
/// </summary>
public class CareerSaveHistoryTests
{
    private static SeasonRecord Season(int year) => new()
    {
        Year = year,
        DriversChampionId = "d1",
        ConstructorsChampionId = "alpha",
        Drivers =
        [
            new DriverSeasonLine { DriverId = "d1", Position = 1, Points = 210, Wins = 7, Podiums = 14 },
            new DriverSeasonLine { DriverId = "d2", Position = 2, Points = 150, Wins = 3, Podiums = 9 },
        ],
        Constructors =
        [
            new ConstructorSeasonLine { TeamId = "alpha", Position = 1, Points = 360, Wins = 10 },
            new ConstructorSeasonLine { TeamId = "bravo", Position = 2, Points = 120, Wins = 0 },
        ],
    };

    private static CareerState WithHistory() => new()
    {
        Seed = 22,
        Date = new DateOnly(2028, 3, 1),
        SeasonHistory = [Season(2025), Season(2026), Season(2027)],
        TrackRecords =
        [
            new TrackRecord { CircuitId = "aaa", BestLapSeconds = 79.512, DriverId = "d1", Year = 2026 },
            new TrackRecord { CircuitId = "bbb", BestLapSeconds = 88.004, DriverId = "d2", Year = 2025 },
        ],
    };

    private static Carset HistoryCarset() => new()
    {
        Id = "mini",
        Name = "Mini",
        Rules = new RulesSet { SeriesName = "S", Points = new PointsScheme { RacePoints = [25, 18] } },
        Circuits = [],
        Drivers = [],
        Calendar = [],
        Teams = [],
        SeasonHistory = [Season(2025)],
        TrackRecords = [new TrackRecord { CircuitId = "aaa", BestLapSeconds = 80.1, DriverId = "d1", Year = 2025 }],
    };

    [Fact]
    public void A_round_trip_preserves_the_season_archive()
    {
        var loaded = CareerStore.Deserialize(CareerStore.Serialize(WithHistory()));

        Assert.Equal(3, loaded.SeasonHistory.Count);
        var y2026 = loaded.SeasonHistory.Single(s => s.Year == 2026);
        Assert.Equal("d1", y2026.DriversChampionId);
        Assert.Equal("alpha", y2026.ConstructorsChampionId);
        Assert.Equal(2, y2026.Drivers.Count);
        Assert.Equal(210, y2026.Drivers[0].Points);
        Assert.Equal(14, y2026.Drivers[0].Podiums);

        Assert.Equal(2, loaded.TrackRecords.Count);
        var aaa = loaded.TrackRecords.Single(t => t.CircuitId == "aaa");
        Assert.Equal(79.512, aaa.BestLapSeconds);
        Assert.Equal(2026, aaa.Year);
    }

    [Fact]
    public void Serialization_stays_byte_stable_with_history()
    {
        var once = CareerStore.Serialize(WithHistory());
        var twice = CareerStore.Serialize(CareerStore.Deserialize(once));
        Assert.Equal(once, twice);
    }

    [Fact]
    public void Capture_and_restore_carry_the_archive_through_a_save()
    {
        var state = CareerState.Capture(HistoryCarset(), new DateOnly(2025, 1, 1), 7);
        Assert.Single(state.SeasonHistory);
        Assert.Single(state.TrackRecords);

        var restored = state.RestoreInto(HistoryCarset() with { SeasonHistory = [], TrackRecords = [] });
        Assert.Single(restored.SeasonHistory);
        Assert.Equal(2025, restored.SeasonHistory[0].Year);
        Assert.Single(restored.TrackRecords);
        Assert.Equal("aaa", restored.TrackRecords[0].CircuitId);
    }

    [Fact]
    public void A_career_with_no_completed_season_captures_no_archive()
    {
        var carset = HistoryCarset() with { SeasonHistory = [], TrackRecords = [] };
        var state = CareerState.Capture(carset, new DateOnly(2025, 1, 1), 7);
        Assert.Empty(state.SeasonHistory);
        Assert.Empty(state.TrackRecords);
    }
}
