using System.Linq;
using LTF.App.Session;
using Xunit;

namespace LTF.App.Tests;

/// <summary>Quick Race (M20g): runs the real deterministic engine and joins the id-only classification back
/// to driver and team names; same inputs reproduce the same result.</summary>
public class QuickRaceServiceTests
{
    private static LTF.Domain.Carset Flagship() => CarsetCatalog.Discover().Find("global-prix")!.Carset;

    [Fact]
    public void Runs_a_race_and_joins_names()
    {
        var result = QuickRaceService.Run(Flagship(), circuitIndex: 0, seed: 2024);

        Assert.NotEmpty(result.Rows);
        Assert.Equal(1, result.Rows[0].Position);
        Assert.NotEmpty(result.Rows[0].Driver); // id joined to a name
        Assert.NotEmpty(result.Rows[0].Team);
        Assert.Equal(result.Rows[0].Driver, result.WinnerName);
        Assert.NotEmpty(result.CircuitName);
    }

    [Fact]
    public void Is_deterministic()
    {
        var a = QuickRaceService.Run(Flagship(), 0, 2024);
        var b = QuickRaceService.Run(Flagship(), 0, 2024);

        Assert.Equal(a.Rows.Select(r => r.Driver), b.Rows.Select(r => r.Driver));
        Assert.Equal(a.WinnerName, b.WinnerName);
    }
}
