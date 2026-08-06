using System.Linq;
using LTF.Domain;
using LTF.Domain.Common;
using LTF.Domain.Management;
using LTF.Simulation;
using Xunit;

namespace LTF.Career.Tests;

public class SeasonEntriesTests
{
    private static Carset WithQualityControl(double influence, int alphaLevel, int bravoLevel)
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 4);
        return carset with
        {
            Rules = carset.Rules with
            {
                Research = carset.Rules.Research with { QualityControlReliabilityInfluence = influence },
            },
            Teams =
            [
                carset.Teams[0] with { Facilities = Qc(alphaLevel) },
                carset.Teams[1] with { Facilities = Qc(bravoLevel) },
            ],
        };
    }

    [Fact]
    public void Without_influence_it_matches_the_plain_entry_list()
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 4);

        var season = SeasonEntries.Build(carset);
        var plain = EntryList.Build(carset);

        Assert.Equal(
            plain.Select(c => $"{c.Id}:{c.Car.Reliability.Value}"),
            season.Select(c => $"{c.Id}:{c.Car.Reliability.Value}"));
    }

    [Fact]
    public void Quality_control_shifts_a_teams_reliability()
    {
        // Influence 5 → alpha (reliability 85, QC level 5) gains 5*(5-3)=10; bravo (70, level 1) loses 10.
        var entries = SeasonEntries.Build(WithQualityControl(influence: 5.0, alphaLevel: 5, bravoLevel: 1));

        Assert.Equal(95, entries.First(e => e.TeamId == "alpha").Car.Reliability.Value);
        Assert.Equal(60, entries.First(e => e.TeamId == "bravo").Car.Reliability.Value);
    }

    [Fact]
    public void The_neutral_level_leaves_reliability_untouched()
    {
        var entries = SeasonEntries.Build(WithQualityControl(influence: 5.0, alphaLevel: 3, bravoLevel: 3));

        Assert.Equal(85, entries.First(e => e.TeamId == "alpha").Car.Reliability.Value);
        Assert.Equal(70, entries.First(e => e.TeamId == "bravo").Car.Reliability.Value);
    }

    [Fact]
    public void The_entry_list_is_deterministic()
    {
        var carset = WithQualityControl(influence: 3.0, alphaLevel: 5, bravoLevel: 2);

        Assert.Equal(Key(SeasonEntries.Build(carset)), Key(SeasonEntries.Build(carset)));
    }

    private static Facilities Qc(int level) => Facilities.Default with { QualityControl = new FacilityLevel(level) };

    private static string Key(IReadOnlyList<Competitor> entries) =>
        string.Join(";", entries.Select(c => $"{c.Id}:{c.Car.Reliability.Value}"));
}
