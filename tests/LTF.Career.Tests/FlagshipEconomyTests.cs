using System.Linq;
using LTF.Content;
using LTF.Domain;
using Xunit;

namespace LTF.Career.Tests;

/// <summary>
/// Runs the shipped flagship carset's economy through a multi-season sweep on CI (M13): the money model
/// should keep every team solvent and no one's cash should run away — the ROADMAP's "economy balances
/// itself" criterion, checked against real content rather than a fixture.
/// </summary>
public class FlagshipEconomyTests
{
    private static Carset Flagship()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "carsets/global-prix.json");
        Assert.True(File.Exists(path), $"missing content file: {path}");
        return CarsetLoader.LoadFromJson(File.ReadAllText(path));
    }

    [Fact]
    public void The_flagship_economy_keeps_every_team_solvent_over_several_seasons()
    {
        var report = EconomySweep.Run(Flagship(), seasons: 5, seed: 1);

        Assert.Equal(10, report.Teams.Count);
        Assert.Equal(0, report.BankruptTeams);            // no mass bankruptcy
        Assert.True(report.MinFinalBalance >= 0, $"a team went bankrupt: {report.MinFinalBalance}");
        Assert.True(report.MaxFinalBalance < 2_000_000_000L, // no runaway hoarding
            $"cash ran away: {report.MaxFinalBalance}");
    }

    [Fact]
    public void The_flagship_economy_sweep_is_deterministic()
    {
        var flagship = Flagship();

        var a = EconomySweep.Run(flagship, seasons: 3, seed: 1);
        var b = EconomySweep.Run(flagship, seasons: 3, seed: 1);

        Assert.Equal(
            string.Join(";", a.Teams.Select(t => $"{t.TeamId}:{t.FinalBalance}")),
            string.Join(";", b.Teams.Select(t => $"{t.TeamId}:{t.FinalBalance}")));
    }
}
