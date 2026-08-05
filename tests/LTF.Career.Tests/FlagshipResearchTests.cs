using System.Linq;
using LTF.Content;
using LTF.Domain;
using Xunit;

namespace LTF.Career.Tests;

/// <summary>
/// Runs the shipped flagship carset's R&amp;D through a multi-season sweep on CI (M14): the tech tree,
/// facilities and staff should move the cars by a measurable, bounded, deterministic amount — the R&amp;D
/// counterpart to <see cref="FlagshipEconomyTests"/>, checked against real content rather than a fixture.
/// </summary>
public class FlagshipResearchTests
{
    private static Carset Flagship()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "carsets/global-prix.json");
        Assert.True(File.Exists(path), $"missing content file: {path}");
        return CarsetLoader.LoadFromJson(File.ReadAllText(path));
    }

    [Fact]
    public void The_flagship_develops_its_cars_over_several_seasons()
    {
        var report = ResearchSweep.Run(Flagship(), seasons: 5, seed: 1);

        Assert.Equal(10, report.Teams.Count);
        Assert.True(report.TotalNodesApproved > 0, "no node was ever approved");
        Assert.True(report.MaxOverallGain > 0, "no team's car improved");
        Assert.All(report.Teams, t => Assert.True(t.MaxOverall <= 100, $"{t.TeamId} ran past the rating ceiling"));
    }

    [Fact]
    public void The_flagship_research_sweep_is_deterministic()
    {
        var flagship = Flagship();

        var a = ResearchSweep.Run(flagship, seasons: 3, seed: 1);
        var b = ResearchSweep.Run(flagship, seasons: 3, seed: 1);

        Assert.Equal(
            string.Join(";", a.Teams.Select(t => $"{t.TeamId}:{t.FinalOverall},{t.NodesApproved}")),
            string.Join(";", b.Teams.Select(t => $"{t.TeamId}:{t.FinalOverall},{t.NodesApproved}")));
    }
}
