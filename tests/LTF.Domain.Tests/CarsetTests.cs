using System.Linq;
using Xunit;

namespace LTF.Domain.Tests;

public class CarsetTests
{
    [Fact]
    public void Minimal_carset_wires_teams_to_their_drivers()
    {
        var carset = Fixtures.Carset();
        var team = Assert.Single(carset.Teams);
        var driverIds = carset.Drivers.Select(d => d.Id).ToHashSet();

        Assert.Equal(2, team.DriverIds.Count);
        Assert.All(team.DriverIds, id => Assert.Contains(id, driverIds));
    }

    [Fact]
    public void Carset_defaults_schema_version_and_optional_pools()
    {
        var carset = Fixtures.Carset();
        Assert.Equal(1, carset.SchemaVersion);
        Assert.Empty(carset.Reserves);
        Assert.Empty(carset.StaffPool);
        Assert.NotNull(carset.Balance);
    }
}
