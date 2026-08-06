using LTF.Domain.Common;
using LTF.Domain.Racing;
using Xunit;

namespace LTF.Domain.Tests;

public class DomainModelTests
{
    [Fact]
    public void Driver_full_name_joins_first_and_last()
    {
        var driver = Fixtures.Driver("ferreira_mateo", "Mateo", "Ferreira");
        Assert.Equal("Mateo Ferreira", driver.FullName);
    }

    [Fact]
    public void Driver_defaults_are_neutral_for_a_debutant()
    {
        var driver = Fixtures.Driver("x", "New", "Comer");
        Assert.Equal(50, driver.Morale.Value);
        Assert.Equal(50, driver.Reputation.Value);
        Assert.Equal(0, driver.Career.Races);
        Assert.Same(DriverCareer.None, driver.Career);
    }

    [Fact]
    public void Driver_overall_is_the_flat_rating_when_all_attributes_are_equal()
    {
        // Attribute weights sum to 1.0, so an all-60 driver rates 60.
        Assert.Equal(60, Fixtures.Attributes(60).Overall);
    }

    [Fact]
    public void Car_overall_is_the_flat_rating_when_all_attributes_are_equal()
    {
        Assert.Equal(70, Fixtures.Car(70).Overall);
    }

    [Theory]
    [InlineData(1, 25)]
    [InlineData(3, 15)]
    [InlineData(10, 1)]
    [InlineData(11, 0)]
    [InlineData(0, 0)]
    public void Points_scheme_awards_by_position(int position, int expected)
    {
        Assert.Equal(expected, Fixtures.Rules().Points.PointsFor(position));
    }

    [Fact]
    public void Circuit_race_distance_is_laps_times_lap_length()
    {
        var circuit = Fixtures.Circuit("belgian", "Belgian Grand Prix");
        Assert.Equal(50 * 5.0, circuit.RaceDistanceKm, 6);
    }

    [Fact]
    public void Records_compare_by_value()
    {
        Assert.Equal(Fixtures.Attributes(60), Fixtures.Attributes(60));
        Assert.NotEqual(Fixtures.Attributes(60), Fixtures.Attributes(61));
    }

    [Fact]
    public void Driver_potential_defaults_to_zero_and_leaves_the_rating_untouched()
    {
        var driver = Fixtures.Driver("x", "New", "Comer");
        Assert.Equal(0, driver.Potential);            // unset sentinel — development is a no-op
        Assert.Equal(60, driver.Attributes.Overall);  // potential does not feed the rating
    }

    [Fact]
    public void Driver_carries_a_hidden_potential_ceiling()
    {
        var driver = Fixtures.Driver("x", "New", "Comer") with { Potential = 88 };
        Assert.Equal(88, driver.Potential);
    }

    [Fact]
    public void Driver_potential_participates_in_value_equality()
    {
        var driver = Fixtures.Driver("x", "New", "Comer");
        Assert.Equal(driver, driver with { Potential = 0 });      // the default is stable
        Assert.NotEqual(driver, driver with { Potential = 90 });  // potential distinguishes drivers
    }
}
