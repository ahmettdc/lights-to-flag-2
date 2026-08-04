using LTF.Domain.Common;
using Xunit;

namespace LTF.Simulation.Tests;

public class WeatherModelTests
{
    [Fact]
    public void Slicks_are_best_in_the_dry()
    {
        Assert.Equal(0.0, WeatherModel.ConditionPenalty(TyreCompound.Medium, 0.0), 6);
        Assert.True(WeatherModel.ConditionPenalty(TyreCompound.Medium, 0.0)
                    < WeatherModel.ConditionPenalty(TyreCompound.Wet, 0.0));
    }

    [Fact]
    public void Slicks_are_disastrous_in_heavy_rain()
    {
        Assert.True(WeatherModel.ConditionPenalty(TyreCompound.Medium, 0.8)
                    > WeatherModel.ConditionPenalty(TyreCompound.Wet, 0.8) + 10.0);
    }

    [Fact]
    public void Intermediates_suit_mixed_conditions_best()
    {
        var mixed = WeatherModel.ConditionPenalty(TyreCompound.Intermediate, 0.45);
        Assert.True(mixed < WeatherModel.ConditionPenalty(TyreCompound.Intermediate, 0.0));
        Assert.True(mixed < WeatherModel.ConditionPenalty(TyreCompound.Intermediate, 0.9));
    }

    [Fact]
    public void Evolve_is_deterministic_and_stays_in_bounds()
    {
        Assert.Equal(RunWeather(new DeterministicRandom(11)), RunWeather(new DeterministicRandom(11)));
    }

    private static double RunWeather(IRandom rng)
    {
        var circuit = SimFixtures.Circuit();
        var track = TrackConditions.Dry;
        for (var i = 0; i < 200; i++)
        {
            track = WeatherModel.Evolve(track, circuit, rng);
            Assert.InRange(track.Wetness, 0.0, 1.0);
        }

        return track.Wetness;
    }
}
