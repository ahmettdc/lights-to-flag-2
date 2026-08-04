using LTF.Domain.Common;
using LTF.Simulation.Laps;
using Xunit;

namespace LTF.Simulation.Tests;

public class LapConditionsTests
{
    private static SectorTimes Lap(LapConditions conditions, int seed = 3) =>
        LapTimeModel.Simulate(
            SimFixtures.Circuit(), SimFixtures.Car(80), SimFixtures.Attributes(80),
            SimFixtures.Balance, conditions, new DeterministicRandom(seed));

    [Fact]
    public void Neutral_conditions_match_the_base_lap()
    {
        var circuit = SimFixtures.Circuit();
        var car = SimFixtures.Car(80);
        var driver = SimFixtures.Attributes(80);
        var balance = SimFixtures.Balance;

        var baseLap = LapTimeModel.Simulate(circuit, car, driver, balance, new DeterministicRandom(4));
        var neutral = LapTimeModel.Simulate(circuit, car, driver, balance, LapConditions.Neutral, new DeterministicRandom(4));

        Assert.Equal(baseLap, neutral);
    }

    [Fact]
    public void A_heavier_fuel_load_is_slower()
    {
        var full = Lap(new LapConditions { Tyre = TyreState.Fresh(TyreCompound.Medium), FuelFraction = 1.0 });
        var empty = Lap(new LapConditions { Tyre = TyreState.Fresh(TyreCompound.Medium), FuelFraction = 0.0 });
        Assert.True(full.Total > empty.Total);
    }

    [Fact]
    public void A_worn_tyre_is_slower_than_a_fresh_one()
    {
        var worn = Lap(new LapConditions { Tyre = new TyreState { Compound = TyreCompound.Medium, Age = 25, Wear = 0.9 } });
        var fresh = Lap(new LapConditions { Tyre = TyreState.Fresh(TyreCompound.Medium) });
        Assert.True(worn.Total > fresh.Total);
    }

    [Fact]
    public void Slicks_in_the_rain_are_far_slower_than_wets()
    {
        var wetTrack = new TrackConditions(0.8);
        var slick = Lap(new LapConditions { Tyre = TyreState.Fresh(TyreCompound.Medium), Track = wetTrack });
        var wets = Lap(new LapConditions { Tyre = TyreState.Fresh(TyreCompound.Wet), Track = wetTrack });
        Assert.True(slick.Total > wets.Total + 10.0);
    }

    [Fact]
    public void Same_seed_and_conditions_are_deterministic()
    {
        var conditions = new LapConditions
        {
            Tyre = new TyreState { Compound = TyreCompound.Soft, Age = 5, Wear = 0.3 },
            FuelFraction = 0.5,
            Track = new TrackConditions(0.2),
        };
        Assert.Equal(Lap(conditions, 7), Lap(conditions, 7));
    }
}
