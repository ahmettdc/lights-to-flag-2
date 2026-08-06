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

    [Fact]
    public void Carset_designates_no_player_team_by_default()
    {
        var carset = Fixtures.Carset();
        Assert.Equal("", carset.PlayerTeamId);
        Assert.Null(carset.PlayerTeam());
    }

    [Fact]
    public void PlayerTeam_resolves_the_designated_team()
    {
        var carset = Fixtures.Carset() with { PlayerTeamId = "talon" };
        var player = carset.PlayerTeam();
        Assert.NotNull(player);
        Assert.Equal("talon", player!.Id);
    }

    [Fact]
    public void PlayerTeam_is_null_when_the_id_matches_no_team()
    {
        var carset = Fixtures.Carset() with { PlayerTeamId = "nonexistent" };
        Assert.Null(carset.PlayerTeam());
    }
}
