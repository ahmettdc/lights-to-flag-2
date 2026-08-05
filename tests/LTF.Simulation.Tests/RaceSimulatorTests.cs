using System.Linq;
using System.Text;
using LTF.Domain.Common;
using LTF.Domain.Racing;
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

    // ---- Traffic & overtaking (M6) ----------------------------------------

    [Fact]
    public void Overtakes_happen_in_a_close_field()
    {
        var carset = SimFixtures.EqualFieldCarset();
        var grid = EntryList.Build(carset);
        var circuit = SimFixtures.Circuit(overtaking: 90);
        var balance = SimFixtures.CalmBalance with
        {
            CombatThresholdSeconds = 3.0,
            OvertakeBaseChance = 1.0,
            DirtyAirLossSeconds = 0.3,
            SlipstreamBoost = 0.4,
            PassMarginSeconds = 0.3,
        };

        var result = RaceSimulator.Run(circuit, grid, carset.Rules, balance, 7);

        var overtakes = result.Events.Where(e => e.Kind == RaceEventKind.Overtake).ToList();
        Assert.NotEmpty(overtakes);
        Assert.All(overtakes, e => Assert.False(string.IsNullOrEmpty(e.OtherCompetitorId)));
    }

    [Fact]
    public void A_hard_to_pass_track_yields_fewer_overtakes()
    {
        var carset = SimFixtures.EqualFieldCarset();
        var grid = EntryList.Build(carset);
        var balance = SimFixtures.CalmBalance with
        {
            CombatThresholdSeconds = 3.0,
            OvertakeBaseChance = 0.8,
            DirtyAirLossSeconds = 0.3,
            SlipstreamBoost = 0.4,
            PassMarginSeconds = 0.3,
        };

        var easy = SimFixtures.Circuit(overtaking: 95);
        var hard = SimFixtures.Circuit(overtaking: 5);

        var easyCount = 0;
        var hardCount = 0;
        for (var seed = 0; seed < 20; seed++)
        {
            easyCount += RaceSimulator.Run(easy, grid, carset.Rules, balance, seed)
                .Events.Count(e => e.Kind == RaceEventKind.Overtake);
            hardCount += RaceSimulator.Run(hard, grid, carset.Rules, balance, seed)
                .Events.Count(e => e.Kind == RaceEventKind.Overtake);
        }

        Assert.True(easyCount > hardCount, $"easy={easyCount} hard={hardCount}");
    }

    [Fact]
    public void Traffic_is_deterministic()
    {
        var carset = SimFixtures.EqualFieldCarset();
        var grid = EntryList.Build(carset);
        var circuit = SimFixtures.Circuit(overtaking: 60);
        var balance = SimFixtures.CalmBalance with { CombatThresholdSeconds = 3.0, OvertakeBaseChance = 0.8 };

        var a = RaceSimulator.Run(circuit, grid, carset.Rules, balance, 99);
        var b = RaceSimulator.Run(circuit, grid, carset.Rules, balance, 99);

        Assert.Equal(
            a.Events.Where(e => e.Kind == RaceEventKind.Overtake).Select(e => (e.Lap, e.CompetitorId, e.OtherCompetitorId)),
            b.Events.Where(e => e.Kind == RaceEventKind.Overtake).Select(e => (e.Lap, e.CompetitorId, e.OtherCompetitorId)));
        Assert.Equal(
            a.Classification.Select(e => (e.CompetitorId, e.TotalTime)),
            b.Classification.Select(e => (e.CompetitorId, e.TotalTime)));
    }

    // ---- Regulation eras: 2026 active aero + Manual Override (26a) ---------

    [Fact]
    public void A_null_regulation_set_matches_the_drs_era()
    {
        var carset = SimFixtures.EqualFieldCarset();
        var grid = EntryList.Build(carset);
        var circuit = SimFixtures.Circuit(overtaking: 70);
        // Traffic and every incident live, so the whole race — overtakes included — is exercised.
        var balance = carset.Balance with { CombatThresholdSeconds = 3.0, OvertakeBaseChance = 0.8 };

        // A DRS-era set carrying deliberately odd 2026 energy fields: they must be ignored entirely.
        var drsWithNoise = new RegulationSet
        {
            Era = RegulationEra.DrsEra,
            EnergyRegenPerLap = 0.99,
            ManualOverrideEnergyCost = 0.99,
            ManualOverrideBoost = 9.0,
            DeRatingThreshold = 0.9,
            DeRatingPenaltySeconds = 99.0,
        };

        var implicitDrs = RaceSimulator.Run(circuit, grid, carset.Rules, balance, 2024);
        var explicitDrs = RaceSimulator.Run(circuit, grid, carset.Rules, balance, 2024, regulations: RegulationSet.Drs);
        var noisyDrs = RaceSimulator.Run(circuit, grid, carset.Rules, balance, 2024, regulations: drsWithNoise);

        Assert.Equal(Digest(implicitDrs), Digest(explicitDrs));
        Assert.Equal(Digest(implicitDrs), Digest(noisyDrs));
    }

    [Fact]
    public void The_2026_era_overtakes_via_manual_override()
    {
        var carset = SimFixtures.EqualFieldCarset();
        var grid = EntryList.Build(carset);
        var circuit = SimFixtures.Circuit(overtaking: 90);
        var balance = SimFixtures.CalmBalance with
        {
            CombatThresholdSeconds = 3.0,
            OvertakeBaseChance = 1.0,
            DirtyAirLossSeconds = 0.3,
            PassMarginSeconds = 0.3,
        };

        var result = RaceSimulator.Run(circuit, grid, carset.Rules, balance, 7, regulations: RegulationSet.Aero2026);

        var overtakes = result.Events.Where(e => e.Kind == RaceEventKind.Overtake).ToList();
        Assert.NotEmpty(overtakes);
        // At least one pass was made on a deployed Manual Override (the 2026 aid).
        Assert.Contains(overtakes, e => e.Description == "Overtake (override)");
    }

    [Fact]
    public void Manual_override_is_limited_by_the_energy_budget()
    {
        var carset = SimFixtures.EqualFieldCarset();
        var grid = EntryList.Build(carset);
        var circuit = SimFixtures.Circuit(overtaking: 60);
        var balance = SimFixtures.CalmBalance with { CombatThresholdSeconds = 3.0, OvertakeBaseChance = 0.8 };

        // Plentiful: the battery refills fully every lap, so the override is always available.
        var plentiful = new RegulationSet
        {
            Era = RegulationEra.ActiveAero2026,
            EnergyRegenPerLap = 1.0,
            ManualOverrideEnergyCost = 0.1,
            ManualOverrideBoost = 0.6,
            DeRatingThreshold = 0.0,
        };
        // Scarce: no regen and a costly override, so the battery empties and the boost dries up.
        // De-rating is switched off so only the override gating drives the difference.
        var scarce = plentiful with { EnergyRegenPerLap = 0.0, ManualOverrideEnergyCost = 0.5 };

        var plentifulCount = 0;
        var scarceCount = 0;
        for (var seed = 0; seed < 20; seed++)
        {
            plentifulCount += RaceSimulator.Run(circuit, grid, carset.Rules, balance, seed, regulations: plentiful)
                .Events.Count(e => e.Kind == RaceEventKind.Overtake);
            scarceCount += RaceSimulator.Run(circuit, grid, carset.Rules, balance, seed, regulations: scarce)
                .Events.Count(e => e.Kind == RaceEventKind.Overtake);
        }

        Assert.True(plentifulCount > scarceCount, $"plentiful={plentifulCount} scarce={scarceCount}");
    }

    [Fact]
    public void Energy_stays_full_in_the_drs_era()
    {
        var carset = SimFixtures.EqualFieldCarset();
        var grid = EntryList.Build(carset);
        var circuit = SimFixtures.Circuit(overtaking: 90);
        var balance = SimFixtures.CalmBalance with { CombatThresholdSeconds = 3.0, OvertakeBaseChance = 1.0 };

        var result = RaceSimulator.Run(circuit, grid, carset.Rules, balance, 7);

        Assert.All(result.Telemetry.Laps, s => Assert.All(s.Order, o => Assert.Equal(1.0, o.Energy)));
    }

    [Fact]
    public void Energy_is_drawn_down_in_2026_traffic()
    {
        var carset = SimFixtures.EqualFieldCarset();
        var grid = EntryList.Build(carset);
        var circuit = SimFixtures.Circuit(overtaking: 90);
        var balance = SimFixtures.CalmBalance with { CombatThresholdSeconds = 3.0, OvertakeBaseChance = 1.0 };

        var result = RaceSimulator.Run(circuit, grid, carset.Rules, balance, 7, regulations: RegulationSet.Aero2026);

        var minEnergy = result.Telemetry.Laps.SelectMany(s => s.Order).Min(o => o.Energy);
        Assert.True(minEnergy < 1.0, $"minEnergy={minEnergy}");
        Assert.True(minEnergy >= 0.0, $"minEnergy={minEnergy}");
    }

    [Fact]
    public void A_2026_race_is_deterministic()
    {
        var carset = SimFixtures.EqualFieldCarset();
        var grid = EntryList.Build(carset);
        var circuit = SimFixtures.Circuit(overtaking: 70);
        var balance = carset.Balance with { CombatThresholdSeconds = 3.0, OvertakeBaseChance = 0.8 };

        var a = RaceSimulator.Run(circuit, grid, carset.Rules, balance, 2024, regulations: RegulationSet.Aero2026);
        var b = RaceSimulator.Run(circuit, grid, carset.Rules, balance, 2024, regulations: RegulationSet.Aero2026);

        Assert.Equal(Digest(a), Digest(b));
    }

    // ---- Active aero (26b) ------------------------------------------------

    [Fact]
    public void Active_aero_makes_the_2026_car_faster_on_a_power_track()
    {
        var carset = SimFixtures.EqualFieldCarset();
        var grid = EntryList.Build(carset);
        var circuit = SimFixtures.Circuit(power: 95, downforce: 40);
        // Pure pace, traffic off: the only difference between the two runs is the active-aero gain.
        var balance = SimFixtures.CalmBalance;

        var withAero = RegulationSet.Aero2026;
        var withoutAero = new RegulationSet
        {
            Era = RegulationEra.ActiveAero2026,
            LowDragLapGainSeconds = 0.0,
            HighDownforceLapGainSeconds = 0.0,
        };

        var fast = RaceSimulator.Run(circuit, grid, carset.Rules, balance, 7, regulations: withAero);
        var slow = RaceSimulator.Run(circuit, grid, carset.Rules, balance, 7, regulations: withoutAero);

        Assert.True(fast.Classification[0].TotalTime < slow.Classification[0].TotalTime,
            $"withAero={fast.Classification[0].TotalTime} withoutAero={slow.Classification[0].TotalTime}");
    }

    [Fact]
    public void Active_aero_gain_is_larger_on_a_power_sensitive_track()
    {
        var carset = SimFixtures.EqualFieldCarset();
        var grid = EntryList.Build(carset);
        var balance = SimFixtures.CalmBalance;

        var withAero = RegulationSet.Aero2026;
        var withoutAero = new RegulationSet
        {
            Era = RegulationEra.ActiveAero2026,
            LowDragLapGainSeconds = 0.0,
            HighDownforceLapGainSeconds = 0.0,
        };

        double Gain(Circuit c)
        {
            var a = RaceSimulator.Run(c, grid, carset.Rules, balance, 7, regulations: withAero).Classification[0].TotalTime;
            var b = RaceSimulator.Run(c, grid, carset.Rules, balance, 7, regulations: withoutAero).Classification[0].TotalTime;
            return b - a; // seconds saved by active aero
        }

        // Same low downforce on both, so the difference is the low-drag (X) gain, which scales with power.
        var powerTrack = SimFixtures.Circuit(power: 95, downforce: 20);
        var flatTrack = SimFixtures.Circuit(power: 20, downforce: 20);

        Assert.True(Gain(powerTrack) > Gain(flatTrack), $"power={Gain(powerTrack)} flat={Gain(flatTrack)}");
    }

    [Fact]
    public void The_2026_low_drag_mode_raises_top_speed()
    {
        var carset = SimFixtures.EqualFieldCarset();
        var grid = EntryList.Build(carset);
        var circuit = SimFixtures.Circuit(power: 90);
        var balance = SimFixtures.CalmBalance;

        var drs = RaceSimulator.Run(circuit, grid, carset.Rules, balance, 7);
        var era2026 = RaceSimulator.Run(circuit, grid, carset.Rules, balance, 7, regulations: RegulationSet.Aero2026);

        Assert.True(era2026.Classification.Max(e => e.TopSpeed) > drs.Classification.Max(e => e.TopSpeed));
    }

    // ---- Pit stops (M7a) --------------------------------------------------

    [Fact]
    public void No_mandatory_stop_means_no_pit_stops()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);

        // Fixture rules mandate zero stops, so a calm race pits nobody.
        var result = RaceSimulator.Run(carset.Circuits[0], grid, carset.Rules, SimFixtures.CalmBalance, 7);

        Assert.DoesNotContain(result.Events, e => e.Kind == RaceEventKind.Pit);
    }

    [Fact]
    public void Cars_pit_the_mandated_number_of_times()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);
        var rules = carset.Rules with { MandatoryPitStops = 2 };

        // Calm balance: every car reaches the flag, so every finisher completes its two stops.
        var result = RaceSimulator.Run(carset.Circuits[0], grid, rules, SimFixtures.CalmBalance, 7);

        var pits = result.Events.Where(e => e.Kind == RaceEventKind.Pit).ToList();
        Assert.NotEmpty(pits);
        foreach (var e in result.Classification.Where(c => c.Status == FinishStatus.Finished))
        {
            Assert.Equal(2, pits.Count(p => p.CompetitorId == e.CompetitorId));
        }
    }

    [Fact]
    public void A_pit_stop_fits_fresh_tyres_of_a_different_compound()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);
        var rules = carset.Rules with { MandatoryPitStops = 1 };

        var result = RaceSimulator.Run(
            carset.Circuits[0], grid, rules, SimFixtures.CalmBalance, 7, startingCompound: TyreCompound.Medium);

        // The stop switches off the starting Medium onto a fresh (age 0) Hard set.
        Assert.Contains(
            result.Telemetry.Laps.SelectMany(s => s.Order),
            o => o.TyreCompound == TyreCompound.Hard && o.TyreAge == 0);
    }

    [Fact]
    public void Pitting_costs_race_time_when_tyres_do_not_wear()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);
        // No tyre wear, so the pit loss is not offset by fresh-tyre pace — the stop is a net loss.
        var balance = SimFixtures.CalmBalance with { TyreWearPerLap = 0.0 };

        var noPit = RaceSimulator.Run(carset.Circuits[0], grid, carset.Rules with { MandatoryPitStops = 0 }, balance, 7);
        var onePit = RaceSimulator.Run(carset.Circuits[0], grid, carset.Rules with { MandatoryPitStops = 1 }, balance, 7);

        Assert.True(onePit.Classification[0].TotalTime > noPit.Classification[0].TotalTime,
            $"onePit={onePit.Classification[0].TotalTime} noPit={noPit.Classification[0].TotalTime}");
    }

    [Fact]
    public void Slow_pit_stops_are_logged_when_certain()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);
        var rules = carset.Rules with { MandatoryPitStops = 1 };
        var balance = SimFixtures.CalmBalance with { SlowPitStopChance = 1.0 };

        var result = RaceSimulator.Run(carset.Circuits[0], grid, rules, balance, 7);

        var pits = result.Events.Where(e => e.Kind == RaceEventKind.Pit).ToList();
        Assert.NotEmpty(pits);
        Assert.All(pits, e => Assert.StartsWith("Slow pit stop", e.Description, StringComparison.Ordinal));
    }

    [Fact]
    public void A_race_with_pit_stops_is_deterministic()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);
        var rules = carset.Rules with { MandatoryPitStops = 2 };

        var a = RaceSimulator.Run(carset.Circuits[0], grid, rules, carset.Balance, 2024);
        var b = RaceSimulator.Run(carset.Circuits[0], grid, rules, carset.Balance, 2024);

        Assert.Equal(Digest(a), Digest(b));
    }

    // ---- Pit strategy (M7b) -----------------------------------------------

    [Fact]
    public void Cars_stagger_their_pit_stops()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);
        var rules = carset.Rules with { MandatoryPitStops = 1 };

        var result = RaceSimulator.Run(carset.Circuits[0], grid, rules, SimFixtures.CalmBalance, 7);

        // The field spreads its single stop over several laps rather than all pitting together.
        var pitLaps = result.Events.Where(e => e.Kind == RaceEventKind.Pit).Select(e => e.Lap).Distinct().ToList();
        Assert.True(pitLaps.Count > 1, $"pit laps: {string.Join(",", pitLaps)}");
    }

    // A field that retires often (so full safety cars come out), with a wide opportunistic window.
    private static BalanceCoefficients SafetyCarPitBalance => SimFixtures.CalmBalance with
    {
        ReliabilityFailureRate = 0.3,
        SafetyCarFromIncidentChance = 5.0,
        VirtualSafetyCarShare = 0.0,
        RedFlagShare = 0.0,
        NeutralizationPitWindowLaps = 25,
    };

    [Fact]
    public void A_car_can_pit_under_a_safety_car()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);
        var rules = carset.Rules with { MandatoryPitStops = 2 };

        var pittedUnderSc = false;
        for (var seed = 0; seed < 25 && !pittedUnderSc; seed++)
        {
            var result = RaceSimulator.Run(carset.Circuits[0], grid, rules, SafetyCarPitBalance, seed);
            var neutralLaps = result.Telemetry.Laps
                .Where(s => s.State != NeutralizationState.Green)
                .Select(s => s.Lap)
                .ToHashSet();
            pittedUnderSc = result.Events.Any(e => e.Kind == RaceEventKind.Pit && neutralLaps.Contains(e.Lap));
        }

        Assert.True(pittedUnderSc, "no car took a stop under a neutralisation across 25 seeds");
    }

    [Fact]
    public void Pitting_under_a_safety_car_is_cheaper()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);
        var rules = carset.Rules with { MandatoryPitStops = 2 };

        // Traffic is off (CalmBalance), so a changed pit loss can't propagate — only the discounted
        // safety-car stops move total time. A bigger discount must lower it.
        double TotalOver(double discount)
        {
            var balance = SafetyCarPitBalance with { NeutralizationPitDiscount = discount };
            var sum = 0.0;
            for (var seed = 0; seed < 15; seed++)
            {
                sum += RaceSimulator.Run(carset.Circuits[0], grid, rules, balance, seed).Classification.Sum(e => e.TotalTime);
            }

            return sum;
        }

        Assert.True(TotalOver(0.3) < TotalOver(1.0), "discounted safety-car stops should reduce total race time");
    }

    [Fact]
    public void A_race_with_safety_car_pit_strategy_is_deterministic()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);
        var rules = carset.Rules with { MandatoryPitStops = 2 };

        var a = RaceSimulator.Run(carset.Circuits[0], grid, rules, SafetyCarPitBalance, 2024);
        var b = RaceSimulator.Run(carset.Circuits[0], grid, rules, SafetyCarPitBalance, 2024);

        Assert.Equal(Digest(a), Digest(b));
    }

    // ---- Penalties (M7c) --------------------------------------------------

    [Fact]
    public void Repeated_off_track_moments_draw_a_track_limits_penalty()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);
        // A high error rate (calm otherwise) so a driver piles up off-track moments and, once past
        // the allowance, earns a track-limits penalty.
        var balance = SimFixtures.CalmBalance with { DriverErrorBaseRate = 0.4 };

        var drew = false;
        for (var seed = 0; seed < 25 && !drew; seed++)
        {
            var result = RaceSimulator.Run(carset.Circuits[0], grid, carset.Rules, balance, seed);
            drew = result.Events.Any(e => e.Kind == RaceEventKind.Penalty
                && e.Description.Contains("Track limits", StringComparison.Ordinal));
        }

        Assert.True(drew, "no track-limits penalty across 25 seeds");
    }

    [Fact]
    public void Track_limits_never_penalise_a_clean_field()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);

        // No driver errors at all (calm), so no off-track moments and never a track-limits penalty.
        var result = RaceSimulator.Run(carset.Circuits[0], grid, carset.Rules, SimFixtures.CalmBalance, 7);

        Assert.DoesNotContain(result.Events, e => e.Kind == RaceEventKind.Penalty);
    }

    [Fact]
    public void An_unsafe_pit_release_draws_a_penalty()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);
        var rules = carset.Rules with { MandatoryPitStops = 1 };
        // Every release is unsafe, so every pitting car earns exactly one unsafe-release penalty.
        var balance = SimFixtures.CalmBalance with { UnsafePitReleaseChance = 1.0 };

        var result = RaceSimulator.Run(carset.Circuits[0], grid, rules, balance, 7);

        var pits = result.Events.Count(e => e.Kind == RaceEventKind.Pit);
        var releases = result.Events.Count(e => e.Kind == RaceEventKind.Penalty
            && e.Description.Contains("Unsafe pit release", StringComparison.Ordinal));
        Assert.True(pits > 0, "expected at least one pit stop");
        Assert.Equal(pits, releases);
    }

    [Fact]
    public void An_unsafe_pit_release_costs_race_time()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);
        var rules = carset.Rules with { MandatoryPitStops = 1 };
        // No tyre wear, traffic off: the same race, so the only difference is the penalty per stop.
        var baseBalance = SimFixtures.CalmBalance with { TyreWearPerLap = 0.0 };

        var safe = RaceSimulator.Run(carset.Circuits[0], grid, rules, baseBalance, 7);
        var unsafeRun = RaceSimulator.Run(
            carset.Circuits[0], grid, rules, baseBalance with { UnsafePitReleaseChance = 1.0 }, 7);

        // Each of the field's cars pits once, so the field's total time rises by exactly one penalty per car.
        var delta = unsafeRun.Classification.Sum(e => e.TotalTime) - safe.Classification.Sum(e => e.TotalTime);
        Assert.Equal(grid.Count * baseBalance.UnsafePitReleasePenaltySeconds, delta, precision: 3);
    }

    [Fact]
    public void A_race_with_penalties_is_deterministic()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);
        var rules = carset.Rules with { MandatoryPitStops = 1 };
        var balance = SimFixtures.CalmBalance with
        {
            DriverErrorBaseRate = 0.4,
            UnsafePitReleaseChance = 0.5,
        };

        var a = RaceSimulator.Run(carset.Circuits[0], grid, rules, balance, 2024);
        var b = RaceSimulator.Run(carset.Circuits[0], grid, rules, balance, 2024);

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
                  .Append(o.Fuel.ToString("R")).Append(',').Append(o.EngineMode).Append(',')
                  .Append(o.Energy.ToString("R")).Append('|');
            }
        }

        return sb.ToString();
    }
}
