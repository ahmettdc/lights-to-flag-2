using System.Collections.Generic;
using System.Linq;
using LTF.Content;
using LTF.Domain;
using LTF.Simulation;
using Xunit;

namespace LTF.Career.Tests;

/// <summary>
/// Runs M15's component, quality-control and test-day content against the shipped flagship carset on CI
/// (the M15 counterpart to <see cref="FlagshipEconomyTests"/>/<see cref="FlagshipResearchTests"/>): real
/// content produces grid penalties, a quality-control reliability lift and test days, all deterministically.
/// </summary>
public class FlagshipComponentTests
{
    private static Carset Flagship()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "carsets/global-prix.json");
        Assert.True(File.Exists(path), $"missing content file: {path}");
        return CarsetLoader.LoadFromJson(File.ReadAllText(path));
    }

    [Fact]
    public void The_flagship_incurs_component_grid_penalties()
    {
        var flagship = Flagship();

        var penalties = ComponentPenalties.ForSeason(flagship);

        Assert.Equal(flagship.Calendar.Count, penalties.Count);
        Assert.Contains(penalties, round => round.Count > 0);            // a car overruns its allocation
        Assert.All(penalties, round => Assert.All(round.Values, p => Assert.True(p > 0)));
    }

    [Fact]
    public void The_flagship_quality_control_lifts_reliability()
    {
        var flagship = Flagship();

        var lifted = SeasonEntries.Build(flagship).First(e => e.TeamId == "talon");
        var plain = EntryList.Build(flagship).First(e => e.TeamId == "talon");

        Assert.True(lifted.Car.Reliability.Value > plain.Car.Reliability.Value); // top quality control helps
    }

    [Fact]
    public void The_flagship_ships_test_days()
    {
        Assert.NotEmpty(Flagship().TestDays);
    }

    [Fact]
    public void The_flagship_component_penalties_are_deterministic()
    {
        var flagship = Flagship();

        Assert.Equal(Key(ComponentPenalties.ForSeason(flagship)), Key(ComponentPenalties.ForSeason(flagship)));
    }

    private static string Key(IReadOnlyList<IReadOnlyDictionary<string, int>> penalties) =>
        string.Join("|", penalties.Select(p =>
            string.Join(",", p.OrderBy(kv => kv.Key, System.StringComparer.Ordinal).Select(kv => $"{kv.Key}:{kv.Value}"))));
}
