using System.Collections.Generic;
using System.Linq;
using LightsToFlag.Core.Simulation;

namespace LightsToFlag.Tests;

public class QualifyingTests
{
    private static readonly Core.Domain.Coefficients Coeff = Fixtures.Coefficients();
    private static readonly Core.Domain.CircuitSpec Circuit = Fixtures.Circuit();
    private static readonly Core.Domain.RulesSet Rules = Fixtures.Rules();

    private static List<Competitor> MixedField()
    {
        // One clear ace (id "1"), five journeymen.
        var field = new List<Competitor>
        {
            Fixtures.Competitor("1", Fixtures.Driver("Ace", pace: 10, consistency: 10, qualifying: 10),
                Fixtures.Team("Top", aero: 10, mech: 10, engine: 10, qualifying: 10)),
        };
        for (var i = 2; i <= 6; i++)
        {
            field.Add(Fixtures.Competitor(i.ToString(),
                Fixtures.Driver($"D{i}", pace: 3, consistency: 4, qualifying: 3),
                Fixtures.Team($"T{i}", aero: 3, mech: 3, engine: 3, qualifying: 3)));
        }

        return field;
    }

    [Fact]
    public void Grid_is_a_permutation_of_the_field()
    {
        var result = QualifyingSimulator.Run(MixedField(), Circuit, Coeff, Rules, new SeededRandom(10));

        Assert.Equal(6, result.Grid.Count);
        Assert.Equal(Enumerable.Range(1, 6), result.Grid.Select(g => g.GridPosition));
        Assert.Equal(6, result.Grid.Select(g => g.CompetitorId).Distinct().Count());
    }

    [Fact]
    public void Same_seed_produces_identical_grid()
    {
        var a = QualifyingSimulator.Run(MixedField(), Circuit, Coeff, Rules, new SeededRandom(42));
        var b = QualifyingSimulator.Run(MixedField(), Circuit, Coeff, Rules, new SeededRandom(42));
        Assert.Equal(a.Grid.Select(g => g.CompetitorId), b.Grid.Select(g => g.CompetitorId));
    }

    [Fact]
    public void The_strongest_car_and_driver_usually_takes_pole()
    {
        var poles = 0;
        for (var seed = 0; seed < 30; seed++)
        {
            var result = QualifyingSimulator.Run(MixedField(), Circuit, Coeff, Rules, new SeededRandom(seed));
            if (result.PolePosition == "1")
            {
                poles++;
            }
        }

        Assert.True(poles >= 27, $"ace took pole {poles}/30 times");
    }
}
