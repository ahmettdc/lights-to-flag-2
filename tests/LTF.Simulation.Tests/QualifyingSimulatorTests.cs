using System.Linq;
using LTF.Domain.Common;
using LTF.Simulation.Qualifying;
using Xunit;

namespace LTF.Simulation.Tests;

public class QualifyingSimulatorTests
{
    [Fact]
    public void Knockout_orders_the_whole_grid()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);

        var result = QualifyingSimulator.Run(carset.Circuits[0], grid, carset.Rules, SimFixtures.Balance, 7);

        Assert.Equal(QualifyingFormat.Knockout, result.Format);
        Assert.Equal(grid.Count, result.Grid.Count);
        Assert.Equal(Enumerable.Range(1, grid.Count), result.Grid.Select(e => e.GridPosition));
        Assert.Equal(grid.Select(c => c.Id).OrderBy(id => id), result.StartingOrder.OrderBy(id => id));
    }

    [Fact]
    public void Knockout_puts_a_dominant_car_on_pole()
    {
        // Fixture: team alpha (car 85) is clearly faster than bravo (70), well beyond lap noise,
        // so a fixture driver of the fast team must take pole from Q3.
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);

        var result = QualifyingSimulator.Run(carset.Circuits[0], grid, carset.Rules, SimFixtures.Balance, 7);

        var pole = result.Grid[0].CompetitorId;
        Assert.Equal(pole, result.PoleCompetitorId);
        Assert.Contains(pole, new[] { "d1", "d2" });
        Assert.Equal(3, result.Grid[0].Part);
    }

    [Fact]
    public void Knockout_eliminates_in_stages()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);

        var result = QualifyingSimulator.Run(carset.Circuits[0], grid, carset.Rules, SimFixtures.Balance, 7);

        // Four cars: Q1 keeps 3, Q2 keeps 2, Q3 decides the top two — so parts run 3,3,2,1 down the grid.
        Assert.Equal(new[] { 3, 3, 2, 1 }, result.Grid.Select(e => e.Part));

        // The deepest part reached never increases as you go down the grid.
        var parts = result.Grid.Select(e => e.Part).ToList();
        for (var i = 1; i < parts.Count; i++)
        {
            Assert.True(parts[i] <= parts[i - 1]);
        }
    }

    [Fact]
    public void Single_lap_orders_by_one_flying_lap()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);
        var rules = carset.Rules with { Qualifying = QualifyingFormat.SingleLap };

        var result = QualifyingSimulator.Run(carset.Circuits[0], grid, rules, SimFixtures.Balance, 7);

        Assert.Equal(QualifyingFormat.SingleLap, result.Format);
        Assert.All(result.Grid, e => Assert.Equal(1, e.Part));
        var times = result.Grid.Select(e => e.BestLap).ToList();
        Assert.Equal(times.OrderBy(t => t), times);
    }

    [Fact]
    public void Single_session_is_never_slower_than_a_single_lap()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);

        // Same seed and streams: a single-session driver's first lap is exactly the single-lap lap,
        // and best-of-several can only match or beat it.
        var single = QualifyingSimulator.Run(
            carset.Circuits[0], grid, carset.Rules with { Qualifying = QualifyingFormat.SingleLap }, SimFixtures.Balance, 7);
        var session = QualifyingSimulator.Run(
            carset.Circuits[0], grid, carset.Rules with { Qualifying = QualifyingFormat.SingleSession }, SimFixtures.Balance, 7);

        var singleById = single.Grid.ToDictionary(e => e.CompetitorId, e => e.BestLap);
        foreach (var e in session.Grid)
        {
            Assert.True(e.BestLap <= singleById[e.CompetitorId] + 1e-9,
                $"{e.CompetitorId}: session {e.BestLap} > single {singleById[e.CompetitorId]}");
        }
    }

    [Fact]
    public void Qualifying_is_deterministic()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);

        var a = QualifyingSimulator.Run(carset.Circuits[0], grid, carset.Rules, SimFixtures.Balance, 2024);
        var b = QualifyingSimulator.Run(carset.Circuits[0], grid, carset.Rules, SimFixtures.Balance, 2024);

        Assert.Equal(Key(a), Key(b));
    }

    private static string Key(QualifyingResult r) =>
        string.Join(";", r.Grid.Select(e => $"{e.GridPosition}|{e.CompetitorId}|{e.Part}|{e.BestLap:R}"));
}
