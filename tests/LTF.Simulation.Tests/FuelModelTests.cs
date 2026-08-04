using Xunit;

namespace LTF.Simulation.Tests;

public class FuelModelTests
{
    [Fact]
    public void More_fuel_costs_more_time()
    {
        var balance = SimFixtures.Balance;
        Assert.True(FuelModel.Penalty(1.0, balance) > FuelModel.Penalty(0.5, balance));
        Assert.True(FuelModel.Penalty(0.5, balance) > FuelModel.Penalty(0.0, balance));
    }

    [Fact]
    public void An_empty_tank_has_no_penalty()
    {
        Assert.Equal(0.0, FuelModel.Penalty(0.0, SimFixtures.Balance), 6);
    }

    [Fact]
    public void Burning_reduces_fuel_by_one_laps_worth()
    {
        var after = FuelModel.Burn(1.0, 50);
        Assert.True(after is < 1.0 and > 0.9);
    }

    [Fact]
    public void Fuel_never_goes_negative()
    {
        Assert.Equal(0.0, FuelModel.Burn(0.0, 50), 6);
    }
}
