using LTF.Domain.Common;
using Xunit;

namespace LTF.Simulation.Tests;

public class TyreModelTests
{
    private static double Delta(TyreCompound compound, int age, double wear)
    {
        var tyre = new TyreState { Compound = compound, Age = age, Wear = wear };
        return TyreModel.TimeDelta(tyre, SimFixtures.Circuit());
    }

    [Fact]
    public void Fresh_mediums_are_neutral()
    {
        Assert.Equal(0.0, Delta(TyreCompound.Medium, 0, 0.0), 6);
    }

    [Fact]
    public void Softer_is_faster_than_harder_when_fresh()
    {
        Assert.True(Delta(TyreCompound.Soft, 0, 0.0) < Delta(TyreCompound.Hard, 0, 0.0));
    }

    [Fact]
    public void Wear_makes_the_tyre_slower()
    {
        Assert.True(Delta(TyreCompound.Medium, 20, 0.2) < Delta(TyreCompound.Medium, 20, 0.6));
        Assert.True(Delta(TyreCompound.Medium, 20, 0.6) < Delta(TyreCompound.Medium, 20, 0.95));
    }

    [Fact]
    public void There_is_a_performance_cliff_near_the_end()
    {
        var atCliff = Delta(TyreCompound.Medium, 20, 0.90) - Delta(TyreCompound.Medium, 20, 0.85);
        var earlier = Delta(TyreCompound.Medium, 20, 0.50) - Delta(TyreCompound.Medium, 20, 0.45);
        Assert.True(atCliff > earlier * 3);
    }

    [Fact]
    public void Better_tyre_management_wears_slower()
    {
        var circuit = SimFixtures.Circuit();
        var tyre = TyreState.Fresh(TyreCompound.Medium);
        var careful = TyreModel.WearRate(tyre, circuit, SimFixtures.Attributes(90), SimFixtures.Balance);
        var rough = TyreModel.WearRate(tyre, circuit, SimFixtures.Attributes(40), SimFixtures.Balance);
        Assert.True(careful < rough);
    }

    [Fact]
    public void Advancing_a_lap_accumulates_wear_and_age()
    {
        var circuit = SimFixtures.Circuit();
        var fresh = TyreState.Fresh(TyreCompound.Soft);
        var used = TyreModel.Advance(fresh, circuit, SimFixtures.Attributes(60), SimFixtures.Balance);

        Assert.Equal(1, used.Age);
        Assert.True(used.Wear > fresh.Wear);
    }
}
