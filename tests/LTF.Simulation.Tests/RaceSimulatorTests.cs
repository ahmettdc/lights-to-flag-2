using System.Linq;
using System.Text;
using LTF.Domain.Common;
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

    // ---- Neutralisation (M5d) ---------------------------------------------

    [Fact]
    public void An_incident_can_bring_out_a_neutralization()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);
        // Retirements are frequent and each one is certain to neutralise.
        var balance = SimFixtures.CalmBalance with { ReliabilityFailureRate = 0.3, SafetyCarFromIncidentChance = 5.0 };

        var result = RaceSimulator.Run(carset.Circuits[0], grid, carset.Rules, balance, 7);

        Assert.Contains(result.Events, e =>
            e.Kind is RaceEventKind.SafetyCar or RaceEventKind.VirtualSafetyCar or RaceEventKind.RedFlag);
        Assert.Contains(result.Telemetry.Laps, s => s.State != NeutralizationState.Green);
    }

    [Fact]
    public void Neutralized_laps_are_calm()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);
        var balance = SimFixtures.CalmBalance with { ReliabilityFailureRate = 0.3, SafetyCarFromIncidentChance = 5.0 };

        var result = RaceSimulator.Run(carset.Circuits[0], grid, carset.Rules, balance, 7);

        var neutralLaps = result.Telemetry.Laps
            .Where(s => s.State != NeutralizationState.Green)
            .Select(s => s.Lap)
            .ToHashSet();
        Assert.NotEmpty(neutralLaps);

        // No racing incident (failure / error / collision) happens on a neutralised lap.
        foreach (var e in result.Events)
        {
            if (e.Kind is RaceEventKind.MechanicalFailure or RaceEventKind.DriverError or RaceEventKind.Collision)
            {
                Assert.DoesNotContain(e.Lap, neutralLaps);
            }
        }
    }

    [Fact]
    public void The_safety_car_compresses_the_field()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);
        // Force a full safety car (no VSC, no red flag) that is certain to be called.
        var balance = SimFixtures.CalmBalance with
        {
            ReliabilityFailureRate = 0.3,
            SafetyCarFromIncidentChance = 5.0,
            VirtualSafetyCarShare = 0.0,
            RedFlagShare = 0.0,
        };

        var result = RaceSimulator.Run(carset.Circuits[0], grid, carset.Rules, balance, 7);

        var safetyCarLaps = result.Telemetry.Laps.Where(s => s.State == NeutralizationState.SafetyCar).ToList();
        Assert.NotEmpty(safetyCarLaps);

        // At the restart the field is bunched nose to tail, so at least one safety-car lap shows
        // a tiny spread.
        Assert.Contains(safetyCarLaps, s => s.Order.Count > 0 && s.Order.Max(o => o.GapToLeader) < 3.0);
    }

    [Fact]
    public void A_neutralized_race_is_deterministic()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);
        var balance = SimFixtures.CalmBalance with { ReliabilityFailureRate = 0.2, SafetyCarFromIncidentChance = 5.0 };

        var a = RaceSimulator.Run(carset.Circuits[0], grid, carset.Rules, balance, 2024);
        var b = RaceSimulator.Run(carset.Circuits[0], grid, carset.Rules, balance, 2024);

        Assert.Equal(
            a.Classification.Select(e => (e.CompetitorId, e.Status, e.Laps, e.TotalTime)),
            b.Classification.Select(e => (e.CompetitorId, e.Status, e.Laps, e.TotalTime)));
        Assert.Equal(
            a.Telemetry.Laps.Select(s => (s.Lap, s.State)),
            b.Telemetry.Laps.Select(s => (s.Lap, s.State)));
        Assert.Contains(a.Telemetry.Laps, s => s.State != NeutralizationState.Green);
    }

    // ---- Telemetry detail (M5e) -------------------------------------------

    [Fact]
    public void Telemetry_samples_carry_sectors_tyres_and_fuel()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);

        var result = RaceSimulator.Run(carset.Circuits[0], grid, carset.Rules, SimFixtures.CalmBalance, 7);

        Assert.All(result.Telemetry.Laps, s => Assert.All(s.Order, o =>
        {
            Assert.Equal(o.LastLap, o.Sector1 + o.Sector2 + o.Sector3, 6); // sectors sum to the lap
            Assert.Equal(TyreCompound.Medium, o.TyreCompound);
            Assert.True(o.Fuel <= 1.0);
            Assert.True(o.IntervalAhead >= 0.0);
        }));
    }

    [Fact]
    public void Top_speed_is_recorded()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);

        var result = RaceSimulator.Run(carset.Circuits[0], grid, carset.Rules, SimFixtures.CalmBalance, 7);

        Assert.All(result.Classification, e => Assert.True(e.TopSpeed > 0.0));
    }

    [Fact]
    public void The_full_telemetry_is_deterministic()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);

        // Full balance: reliability, incidents and neutralisations all live. The whole rich
        // telemetry + event log + classification must reproduce bit for bit — an in-process
        // "golden" (a checked-in golden file waits until local execution is available).
        var a = RaceSimulator.Run(carset.Circuits[0], grid, carset.Rules, carset.Balance, 2024);
        var b = RaceSimulator.Run(carset.Circuits[0], grid, carset.Rules, carset.Balance, 2024);

        Assert.Equal(Digest(a), Digest(b));
    }

    private static string Digest(RaceResult r)
    {
        var sb = new StringBuilder();
        foreach (var e in r.Classification)
        {
            sb.Append(e.Position).Append('|').Append(e.CompetitorId).Append('|').Append(e.Status)
              .Append('|').Append(e.Laps).Append('|').Append(e.TotalTime.ToString("R"))
              .Append('|').Append(e.TopSpeed.ToString("R")).Append('|').Append(e.Points).Append(';');
        }

        foreach (var ev in r.Events)
        {
            sb.Append(ev.Kind).Append('|').Append(ev.Lap).Append('|').Append(ev.CompetitorId)
              .Append('|').Append(ev.OtherCompetitorId ?? "-").Append('|').Append(ev.Description).Append(';');
        }

        foreach (var s in r.Telemetry.Laps)
        {
            sb.Append('L').Append(s.Lap).Append('#').Append(s.State).Append(':');
            foreach (var o in s.Order)
            {
                sb.Append(o.Position).Append(',').Append(o.CompetitorId).Append(',')
                  .Append(o.TotalTime.ToString("R")).Append(',').Append(o.IntervalAhead.ToString("R")).Append(',')
                  .Append(o.LastLap.ToString("R")).Append(',')
                  .Append(o.Sector1.ToString("R")).Append(',').Append(o.Sector2.ToString("R")).Append(',')
                  .Append(o.Sector3.ToString("R")).Append(',')
                  .Append(o.TyreCompound).Append(',').Append(o.TyreWear.ToString("R")).Append(',')
                  .Append(o.Fuel.ToString("R")).Append(',').Append(o.EngineMode).Append('|');
            }
        }

        return sb.ToString();
    }
}
