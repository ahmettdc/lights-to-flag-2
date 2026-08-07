using System.Linq;
using LTF.Career;
using LTF.Domain;
using LTF.Domain.Common;
using LTF.Domain.Racing;
using Xunit;

namespace LTF.Persistence.Tests;

/// <summary>
/// Player race-strategy persistence (M23b): the player's per-round starting-compound choices are a mutation,
/// not shipped content, so they are captured and restored through a save. Empty by default → a career with no
/// chosen strategy is byte-identical. Value-typed (int + string + enum) → round-trips byte-stably.
/// </summary>
public class CareerSaveStrategyTests
{
    private static CareerState WithStrategies() => new()
    {
        Seed = 21,
        Date = new DateOnly(2025, 6, 1),
        PlayerRaceStrategies =
        [
            new RaceStrategy { Round = 1, DriverId = "d1", Compound = TyreCompound.Soft },
            new RaceStrategy { Round = 3, DriverId = "d2", Compound = TyreCompound.Hard },
        ],
    };

    private static Carset StrategyCarset() => new()
    {
        Id = "mini",
        Name = "Mini",
        Rules = new RulesSet { SeriesName = "S", Points = new PointsScheme { RacePoints = [25, 18] } },
        Circuits = [],
        Drivers = [],
        Calendar = [],
        Teams = [],
        PlayerRaceStrategies = [new RaceStrategy { Round = 2, DriverId = "d3", Compound = TyreCompound.Medium }],
    };

    [Fact]
    public void A_round_trip_preserves_the_race_strategies()
    {
        var loaded = CareerStore.Deserialize(CareerStore.Serialize(WithStrategies()));

        Assert.Equal(2, loaded.PlayerRaceStrategies.Count);
        var r1 = loaded.PlayerRaceStrategies.Single(s => s.Round == 1);
        Assert.Equal("d1", r1.DriverId);
        Assert.Equal(TyreCompound.Soft, r1.Compound);
        var r3 = loaded.PlayerRaceStrategies.Single(s => s.Round == 3);
        Assert.Equal(TyreCompound.Hard, r3.Compound);
    }

    [Fact]
    public void Serialization_stays_byte_stable_with_strategies()
    {
        var once = CareerStore.Serialize(WithStrategies());
        var twice = CareerStore.Serialize(CareerStore.Deserialize(once));
        Assert.Equal(once, twice);
    }

    [Fact]
    public void Capture_and_restore_carry_the_strategies_through_a_save()
    {
        var state = CareerState.Capture(StrategyCarset(), new DateOnly(2025, 1, 1), 7);
        Assert.Single(state.PlayerRaceStrategies);

        // Restore onto a freshly loaded (strategy-free) carset — the save is authoritative for the choice.
        var restored = state.RestoreInto(StrategyCarset() with { PlayerRaceStrategies = [] });
        Assert.Single(restored.PlayerRaceStrategies);
        Assert.Equal("d3", restored.PlayerRaceStrategies[0].DriverId);
        Assert.Equal(TyreCompound.Medium, restored.PlayerRaceStrategies[0].Compound);
    }

    [Fact]
    public void A_carset_without_strategies_captures_none()
    {
        var carset = StrategyCarset() with { PlayerRaceStrategies = [] };
        Assert.Empty(CareerState.Capture(carset, new DateOnly(2025, 1, 1), 7).PlayerRaceStrategies);
    }
}
