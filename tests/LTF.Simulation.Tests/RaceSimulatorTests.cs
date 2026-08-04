using System.Linq;
using LTF.Simulation.Racing;
using Xunit;

namespace LTF.Simulation.Tests;

public class RaceSimulatorTests
{
    [Fact]
    public void A_race_is_deterministic()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);

        var a = RaceSimulator.Run(carset.Circuits[0], grid, carset.Rules, carset.Balance, 42);
        var b = RaceSimulator.Run(carset.Circuits[0], grid, carset.Rules, carset.Balance, 42);

        Assert.Equal(
            a.Classification.Select(e => e.CompetitorId),
            b.Classification.Select(e => e.CompetitorId));
        Assert.Equal(a.Classification[0].TotalTime, b.Classification[0].TotalTime);
        Assert.Equal(a.FastestLapTime, b.FastestLapTime);
    }

    [Fact]
    public void Every_car_is_classified_in_a_contiguous_order()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);

        var result = RaceSimulator.Run(carset.Circuits[0], grid, carset.Rules, carset.Balance, 7);

        Assert.Equal(grid.Count, result.Classification.Count);
        for (var i = 0; i < result.Classification.Count; i++)
        {
            Assert.Equal(i + 1, result.Classification[i].Position);
            Assert.Equal(FinishStatus.Finished, result.Classification[i].Status);
        }
    }

    [Fact]
    public void Classification_is_ordered_by_race_time()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);

        var result = RaceSimulator.Run(carset.Circuits[0], grid, carset.Rules, carset.Balance, 7);

        for (var i = 1; i < result.Classification.Count; i++)
        {
            Assert.True(result.Classification[i - 1].TotalTime <= result.Classification[i].TotalTime);
        }
    }

    [Fact]
    public void The_faster_cars_finish_at_the_front()
    {
        // In the fixture, team "alpha" (car 85, drivers d1/d2) is clearly quicker than
        // "bravo" (car 70, d3/d4), so the alpha pair should take the top two places.
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);

        var result = RaceSimulator.Run(carset.Circuits[0], grid, carset.Rules, carset.Balance, 7);

        var topTwo = result.Classification.Take(2).Select(e => e.CompetitorId).ToHashSet();
        Assert.Contains("d1", topTwo);
        Assert.Contains("d2", topTwo);
    }

    [Fact]
    public void The_winner_scores_the_top_points()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);

        var result = RaceSimulator.Run(carset.Circuits[0], grid, carset.Rules, carset.Balance, 7);

        Assert.Equal(carset.Rules.Points.PointsFor(1), result.Classification[0].Points);
        Assert.Equal(result.Classification[0].CompetitorId, result.WinnerId);
    }

    [Fact]
    public void Telemetry_records_one_snapshot_per_lap()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);

        var result = RaceSimulator.Run(carset.Circuits[0], grid, carset.Rules, carset.Balance, 7);

        Assert.Equal(carset.Circuits[0].Laps, result.Telemetry.Laps.Count);
        Assert.All(result.Telemetry.Laps, s => Assert.Equal(grid.Count, s.Order.Count));
    }
}
