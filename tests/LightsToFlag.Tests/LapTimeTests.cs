using System.Linq;
using LightsToFlag.Core.Simulation;

namespace LightsToFlag.Tests;

public class LapTimeTests
{
    private static readonly Core.Domain.Coefficients Coeff = Fixtures.Coefficients();

    [Fact]
    public void Same_seed_produces_identical_lap_sequences()
    {
        var circuit = Fixtures.Circuit();
        var ctx = Fixtures.Lap(Fixtures.Competitor(), circuit, Coeff, fuelLaps: 30, wear: 20);

        var rngA = Seed(7);
        var seqA = Enumerable.Range(0, 50).Select(_ => LapTimeCalculator.WithNoise(ctx, rngA)).ToArray();
        var rngB = Seed(7);
        var seqB = Enumerable.Range(0, 50).Select(_ => LapTimeCalculator.WithNoise(ctx, rngB)).ToArray();

        Assert.Equal(seqA, seqB);
    }

    [Fact]
    public void More_fuel_is_slower()
    {
        var circuit = Fixtures.Circuit();
        var light = LapTimeCalculator.Deterministic(Fixtures.Lap(Fixtures.Competitor(), circuit, Coeff, fuelLaps: 2));
        var heavy = LapTimeCalculator.Deterministic(Fixtures.Lap(Fixtures.Competitor(), circuit, Coeff, fuelLaps: 50));
        Assert.True(heavy > light, $"heavy {heavy} should exceed light {light}");
    }

    [Fact]
    public void More_tyre_wear_is_slower()
    {
        var circuit = Fixtures.Circuit();
        var fresh = LapTimeCalculator.Deterministic(Fixtures.Lap(Fixtures.Competitor(), circuit, Coeff, wear: 0));
        var worn = LapTimeCalculator.Deterministic(Fixtures.Lap(Fixtures.Competitor(), circuit, Coeff, wear: 90));
        Assert.True(worn > fresh);
    }

    [Fact]
    public void Better_car_is_faster()
    {
        var circuit = Fixtures.Circuit();
        var weak = Fixtures.Competitor(team: Fixtures.Team(aero: 2, mech: 2, engine: 2));
        var strong = Fixtures.Competitor(team: Fixtures.Team(aero: 10, mech: 10, engine: 10));

        var weakLap = LapTimeCalculator.Deterministic(Fixtures.Lap(weak, circuit, Coeff));
        var strongLap = LapTimeCalculator.Deterministic(Fixtures.Lap(strong, circuit, Coeff));
        Assert.True(strongLap < weakLap);
    }

    [Fact]
    public void Better_driver_is_faster()
    {
        var circuit = Fixtures.Circuit();
        var rookie = Fixtures.Competitor(driver: Fixtures.Driver(pace: 2));
        var ace = Fixtures.Competitor(driver: Fixtures.Driver(pace: 10));

        var rookieLap = LapTimeCalculator.Deterministic(Fixtures.Lap(rookie, circuit, Coeff));
        var aceLap = LapTimeCalculator.Deterministic(Fixtures.Lap(ace, circuit, Coeff));
        Assert.True(aceLap < rookieLap);
    }

    [Fact]
    public void Slicks_in_the_wet_are_much_slower_than_wets()
    {
        var circuit = Fixtures.Circuit();
        var slicks = LapTimeCalculator.Deterministic(
            Fixtures.Lap(Fixtures.Competitor(), circuit, Coeff, tyre: TyreCompound.Hard, wetness: 0.8));
        var wets = LapTimeCalculator.Deterministic(
            Fixtures.Lap(Fixtures.Competitor(), circuit, Coeff, tyre: TyreCompound.Wet, wetness: 0.8));
        Assert.True(slicks > wets + 3.0);
    }

    [Fact]
    public void Gaussian_has_unit_statistics()
    {
        var rng = Seed(123);
        var samples = Enumerable.Range(0, 20000).Select(_ => rng.NextGaussian()).ToArray();
        var mean = samples.Average();
        var std = Math.Sqrt(samples.Select(x => (x - mean) * (x - mean)).Average());

        Assert.True(Math.Abs(mean) < 0.05, $"mean {mean}");
        Assert.True(Math.Abs(std - 1.0) < 0.05, $"std {std}");
    }

    [Fact]
    public void Smoother_driver_wears_tyres_less()
    {
        var circuit = Fixtures.Circuit(tyreWear: 8);
        var rough = TyreModel.WearPerLap(Fixtures.Competitor(driver: Fixtures.Driver(smoothness: 2)), circuit, TyreCompound.Hard);
        var smooth = TyreModel.WearPerLap(Fixtures.Competitor(driver: Fixtures.Driver(smoothness: 10)), circuit, TyreCompound.Hard);
        Assert.True(smooth < rough);
    }

    [Fact]
    public void Softer_compound_wears_faster()
    {
        var circuit = Fixtures.Circuit(tyreWear: 6);
        var soft = TyreModel.WearPerLap(Fixtures.Competitor(), circuit, TyreCompound.Soft);
        var hard = TyreModel.WearPerLap(Fixtures.Competitor(), circuit, TyreCompound.Hard);
        Assert.True(soft > hard);
    }

    private static IRandom Seed(int seed) => new SeededRandom(seed);
}
