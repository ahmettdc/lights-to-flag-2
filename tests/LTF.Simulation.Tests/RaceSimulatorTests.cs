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

        // Default balance has every event type live.
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

        // Calm balance: pure pace, so every car reaches the flag.
        var result = RaceSimulator.Run(carset.Circuits[0], grid, carset.Rules, SimFixtures.CalmBalance, 7);

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

        var result = RaceSimulator.Run(carset.Circuits[0], grid, carset.Rules, SimFixtures.CalmBalance, 7);

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

        var result = RaceSimulator.Run(carset.Circuits[0], grid, carset.Rules, SimFixtures.CalmBalance, 7);

        var topTwo = result.Classification.Take(2).Select(e => e.CompetitorId).ToHashSet();
        Assert.Contains("d1", topTwo);
        Assert.Contains("d2", topTwo);
    }

    [Fact]
    public void The_winner_scores_the_top_points()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);

        var result = RaceSimulator.Run(carset.Circuits[0], grid, carset.Rules, SimFixtures.CalmBalance, 7);

        Assert.Equal(carset.Rules.Points.PointsFor(1), result.Classification[0].Points);
        Assert.Equal(result.Classification[0].CompetitorId, result.WinnerId);
    }

    [Fact]
    public void Telemetry_records_one_snapshot_per_lap()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);

        // Calm balance keeps every car running, so each lap snapshot holds the whole field.
        var result = RaceSimulator.Run(carset.Circuits[0], grid, carset.Rules, SimFixtures.CalmBalance, 7);

        Assert.Equal(carset.Circuits[0].Laps, result.Telemetry.Laps.Count);
        Assert.All(result.Telemetry.Laps, s => Assert.Equal(grid.Count, s.Order.Count));
    }

    // ---- Reliability (M5b) — isolated from other incidents via CalmBalance ----

    [Fact]
    public void Reliability_failures_retire_cars_with_a_reason_and_event()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);
        var balance = SimFixtures.CalmBalance with { ReliabilityFailureRate = 0.1 };

        var result = RaceSimulator.Run(carset.Circuits[0], grid, carset.Rules, balance, 7);

        var retired = result.Classification.Where(e => e.Status == FinishStatus.Retired).ToList();
        Assert.NotEmpty(retired);
        Assert.All(retired, e => Assert.False(string.IsNullOrEmpty(e.RetirementReason)));

        Assert.NotEmpty(result.Events);
        Assert.All(result.Events, ev => Assert.Equal(RaceEventKind.MechanicalFailure, ev.Kind));

        // Every retirement has a matching failure event.
        foreach (var e in retired)
        {
            Assert.Contains(result.Events, ev => ev.CompetitorId == e.CompetitorId);
        }
    }

    [Fact]
    public void Retirements_are_classified_behind_finishers()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);
        var balance = SimFixtures.CalmBalance with { ReliabilityFailureRate = 0.05 };

        var result = RaceSimulator.Run(carset.Circuits[0], grid, carset.Rules, balance, 3);

        // Once a retirement appears in the order, no finisher may follow it.
        var seenRetired = false;
        foreach (var e in result.Classification)
        {
            if (e.Status == FinishStatus.Retired)
            {
                seenRetired = true;
            }
            else
            {
                Assert.False(seenRetired, "a finisher was classified behind a retirement");
            }
        }
    }

    [Fact]
    public void More_reliable_cars_retire_less_often()
    {
        var carset = SimFixtures.ReliabilityContrastCarset();
        var grid = EntryList.Build(carset);
        var balance = SimFixtures.CalmBalance with { ReliabilityFailureRate = 0.01, ComponentHealthLossPerLap = 0.006 };

        var hardy = 0;
        var fragile = 0;
        for (var seed = 0; seed < 40; seed++)
        {
            var result = RaceSimulator.Run(carset.Circuits[0], grid, carset.Rules, balance, seed);
            foreach (var e in result.Classification)
            {
                if (e.Status != FinishStatus.Retired)
                {
                    continue;
                }

                if (e.CompetitorId is "h1" or "h2")
                {
                    hardy++;
                }
                else
                {
                    fragile++;
                }
            }
        }

        Assert.True(fragile > hardy, $"fragile={fragile} hardy={hardy}");
    }

    // ---- Incidents (M5c) --------------------------------------------------

    [Fact]
    public void Driver_errors_produce_events()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);
        var balance = SimFixtures.CalmBalance with { DriverErrorBaseRate = 0.2 };

        var result = RaceSimulator.Run(carset.Circuits[0], grid, carset.Rules, balance, 7);

        Assert.Contains(result.Events, e => e.Kind == RaceEventKind.DriverError);
    }

    [Fact]
    public void Collisions_name_both_cars_involved()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);
        var balance = SimFixtures.CalmBalance with { CollisionBaseRate = 0.2 };

        var result = RaceSimulator.Run(carset.Circuits[0], grid, carset.Rules, balance, 5);

        var collisions = result.Events.Where(e => e.Kind == RaceEventKind.Collision).ToList();
        Assert.NotEmpty(collisions);
        // At least one collision was with the car ahead (a named second party).
        Assert.Contains(collisions, e => e.OtherCompetitorId is not null);
    }

    [Fact]
    public void Every_car_gets_a_start_incident_when_it_is_certain()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);
        var balance = SimFixtures.CalmBalance with { StartIncidentRate = 1.0 };

        var result = RaceSimulator.Run(carset.Circuits[0], grid, carset.Rules, balance, 1);

        var starts = result.Events.Where(e => e.Kind == RaceEventKind.StartIncident).ToList();
        Assert.Equal(grid.Count, starts.Count);
        Assert.All(starts, e => Assert.Equal(0, e.Lap));
    }

    [Fact]
    public void A_full_event_race_is_deterministic()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);

        // Default balance has reliability, driver errors, collisions and start incidents all live.
        var a = RaceSimulator.Run(carset.Circuits[0], grid, carset.Rules, carset.Balance, 2024);
        var b = RaceSimulator.Run(carset.Circuits[0], grid, carset.Rules, carset.Balance, 2024);

        Assert.Equal(
            a.Classification.Select(e => (e.CompetitorId, e.Status, e.Laps, e.TotalTime)),
            b.Classification.Select(e => (e.CompetitorId, e.Status, e.Laps, e.TotalTime)));
        Assert.Equal(
            a.Events.Select(e => (e.Kind, e.Lap, e.CompetitorId, e.OtherCompetitorId, e.Description)),
            b.Events.Select(e => (e.Kind, e.Lap, e.CompetitorId, e.OtherCompetitorId, e.Description)));
    }
}
