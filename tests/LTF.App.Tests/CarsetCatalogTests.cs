using System.Linq;
using LTF.App.Session;
using Xunit;

namespace LTF.App.Tests;

/// <summary>
/// Carset discovery (M20b): both bundled carsets are found and projected to the summaries the new-career
/// flow lists. Reads the carsets copied next to the test binaries (LTF.App.Tests.csproj).
/// </summary>
public class CarsetCatalogTests
{
    [Fact]
    public void Discovers_both_bundled_carsets()
    {
        var ids = CarsetCatalog.Discover().Descriptors.Select(d => d.Summary.Id).ToList();

        Assert.Contains("global-prix", ids);
        Assert.Contains("global-prix-2026", ids);
    }

    [Fact]
    public void Flagship_summary_names_talon_as_the_default_player_team()
    {
        var flagship = CarsetCatalog.Discover().Find("global-prix");

        Assert.NotNull(flagship);
        Assert.Equal("talon", flagship!.Summary.DefaultPlayerTeamId);
        Assert.Contains(flagship.Summary.Teams, t => t.Id == "talon");
        Assert.All(flagship.Summary.Teams, t => Assert.NotEmpty(t.Name));
    }

    [Fact]
    public void The_2026_carset_ships_no_default_player_team()
    {
        var era = CarsetCatalog.Discover().Find("global-prix-2026");

        Assert.NotNull(era);
        Assert.Equal("", era!.Summary.DefaultPlayerTeamId);
        Assert.NotEmpty(era.Summary.Teams);
    }
}
