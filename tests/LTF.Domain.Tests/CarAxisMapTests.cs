using LTF.Domain.Rnd;
using Xunit;

namespace LTF.Domain.Tests;

public class CarAxisMapTests
{
    [Theory]
    [InlineData(CarAxis.AeroLowSpeed, CarRatingTarget.Aerodynamics)]
    [InlineData(CarAxis.AeroHighSpeed, CarRatingTarget.Aerodynamics)]
    [InlineData(CarAxis.AeroFloor, CarRatingTarget.Aerodynamics)]
    [InlineData(CarAxis.DragEfficiency, CarRatingTarget.Aerodynamics)]
    [InlineData(CarAxis.MechanicalGrip, CarRatingTarget.Chassis)]
    [InlineData(CarAxis.Braking, CarRatingTarget.Chassis)]
    [InlineData(CarAxis.PowerUnit, CarRatingTarget.PowerUnit)]
    [InlineData(CarAxis.TyreManagement, CarRatingTarget.TyreGentleness)]
    [InlineData(CarAxis.PowerUnitReliability, CarRatingTarget.Reliability)]
    [InlineData(CarAxis.GearboxReliability, CarRatingTarget.Reliability)]
    [InlineData(CarAxis.PitOperations, CarRatingTarget.None)]
    public void Each_axis_resolves_to_its_live_car_rating(CarAxis axis, CarRatingTarget expected)
    {
        Assert.Equal(expected, CarAxisMap.TargetOf(axis));
    }

    [Fact]
    public void Every_axis_has_a_defined_target()
    {
        foreach (var axis in Enum.GetValues<CarAxis>())
        {
            Assert.True(Enum.IsDefined(CarAxisMap.TargetOf(axis))); // total mapping, no gaps
        }
    }

    [Fact]
    public void An_empty_tree_has_no_departments_or_nodes()
    {
        Assert.Empty(TechTree.Empty.Departments);
        Assert.Empty(TechTree.Empty.Nodes);
    }
}
