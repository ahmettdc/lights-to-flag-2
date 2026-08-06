using System.Linq;
using LTF.Content;
using LTF.Domain;
using Xunit;

namespace LTF.Career.Tests;

/// <summary>
/// Runs M18's world layer against the shipped flagship carset on CI (the M18 counterpart to
/// <see cref="FlagshipBossTests"/>): the flagship opts into driver development and ships a reserve pool, and
/// a multi-season world sweep ages the grid, retires the oldest drivers, and refills their seats — all
/// deterministically and bounded. The existing flagship Economy/Research/Boss/Component tests guard the race
/// engine, which reads none of the new fields.
/// </summary>
public class FlagshipWorldTests
{
    private static Carset Flagship()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "carsets/global-prix.json");
        Assert.True(File.Exists(path), $"missing content file: {path}");
        return CarsetLoader.LoadFromJson(File.ReadAllText(path));
    }

    [Fact]
    public void The_flagship_opts_into_development_and_ships_a_reserve_pool()
    {
        var flagship = Flagship();

        Assert.True(flagship.Rules.DriverDevelopment.IsActive);
        Assert.NotEmpty(flagship.Reserves);
        Assert.Contains(flagship.Reserves, d => d.Potential > 0); // rookies carry a hidden ceiling
    }

    [Fact]
    public void A_world_sweep_ages_retires_and_refills_the_flagship_grid()
    {
        var report = WorldSweep.Run(Flagship(), seasons: 6, seed: 7);

        Assert.True(report.Retirements >= 1);  // the oldest driver ages out over six seasons
        Assert.True(report.Debuts >= 1);       // and is replaced from the pool
        Assert.True(report.AllSeatsFilled);    // every one of the ten teams keeps its seats
        Assert.True(report.OldestAge < 40);    // no one races at or past the retirement age
        Assert.True(report.YoungestAge >= 18);
    }

    [Fact]
    public void The_flagship_world_sweep_is_deterministic()
    {
        var flagship = Flagship();

        Assert.Equal(Key(WorldSweep.Run(flagship, 6, 7)), Key(WorldSweep.Run(flagship, 6, 7)));
    }

    private static string Key(WorldSweepReport r) =>
        $"{r.Retirements}:{r.Debuts}:{r.OldestAge}:{r.YoungestAge}:" +
        string.Join("|", r.Teams.Select(t => $"{t.TeamId}={t.FinalOverall}/[{string.Join(",", t.DriverIds)}]"));
}
