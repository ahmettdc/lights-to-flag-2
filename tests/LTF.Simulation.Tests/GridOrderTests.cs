using System.Collections.Generic;
using System.Linq;
using LTF.Simulation.Racing;
using Xunit;

namespace LTF.Simulation.Tests;

public class GridOrderTests
{
    [Fact]
    public void Reversed_flips_the_field()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);

        var reversed = GridOrder.Reversed(grid);

        Assert.Equal(grid.Select(c => c.Id).Reverse(), reversed.Select(c => c.Id));
    }

    [Fact]
    public void Reversed_is_a_permutation_of_the_field()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);

        var reversed = GridOrder.Reversed(grid);

        Assert.Equal(grid.Count, reversed.Count);
        Assert.Equal(grid.Select(c => c.Id).OrderBy(x => x), reversed.Select(c => c.Id).OrderBy(x => x));
    }

    [Fact]
    public void A_partial_reverse_flips_only_the_front()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset); // d1, d2, d3, d4

        var reversed = GridOrder.PartiallyReversed(grid, 2);

        // Front two swapped, the rest unchanged.
        Assert.Equal(new[] { "d2", "d1", "d3", "d4" }, reversed.Select(c => c.Id));
    }

    [Fact]
    public void A_grid_penalty_drops_a_car_back()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset); // d1, d2, d3, d4

        var penalised = GridOrder.WithPenalties(grid, new Dictionary<string, int> { ["d1"] = 2 });

        // d1 drops two places, the cars it passes keep their slots.
        Assert.Equal(new[] { "d2", "d3", "d1", "d4" }, penalised.Select(c => c.Id));
    }

    [Fact]
    public void A_reversed_grid_still_runs_deterministically()
    {
        var carset = SimFixtures.Carset();
        var grid = GridOrder.Reversed(EntryList.Build(carset));

        var a = RaceSimulator.Run(carset.Circuits[0], grid, carset.Rules, SimFixtures.Balance, 2024);
        var b = RaceSimulator.Run(carset.Circuits[0], grid, carset.Rules, SimFixtures.Balance, 2024);

        Assert.Equal(
            a.Classification.Select(e => (e.Position, e.CompetitorId, e.TotalTime)),
            b.Classification.Select(e => (e.Position, e.CompetitorId, e.TotalTime)));
    }
}
