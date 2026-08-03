using System.Collections.Generic;
using System.Linq;
using LightsToFlag.Core.Data;
using LightsToFlag.Core.Domain;
using LightsToFlag.Core.Simulation;

namespace LightsToFlag.Tests;

public class RaceTelemetryTests
{
    private static Carset LoadCarset() =>
        new LegacyTextCarsetLoader().Load(TestCarsets.Path(TestCarsets.F1_2019));

    private static RaceTelemetry RunReal(int seed)
    {
        var carset = LoadCarset();
        var competitors = EntryList.BuildFromCarset(carset);
        var circuit = carset.Circuits[0];
        var rng = new SeededRandom(seed);
        var grid = QualifyingSimulator.Run(competitors, circuit, carset.Coefficients, carset.Rules, rng);
        return new RaceSimulator().RunWithTelemetry(competitors, grid, circuit, carset.Coefficients, carset.Rules, rng);
    }

    [Fact]
    public void Telemetry_has_one_snapshot_per_lap()
    {
        var telemetry = RunReal(seed: 1);
        Assert.Equal(telemetry.Final.TotalLaps, telemetry.Laps.Count);
        Assert.Equal(Enumerable.Range(1, telemetry.Laps.Count), telemetry.Laps.Select(s => s.Lap));
    }

    [Fact]
    public void Every_snapshot_lists_the_whole_field_with_unique_positions()
    {
        var telemetry = RunReal(seed: 2);
        var fieldSize = telemetry.Final.Entries.Count;
        foreach (var snap in telemetry.Laps)
        {
            Assert.Equal(fieldSize, snap.Order.Count);
            Assert.Equal(Enumerable.Range(1, fieldSize), snap.Order.Select(o => o.Position));
            Assert.Equal(0.0, snap.Order[0].GapToLeaderSeconds);
            Assert.All(snap.Order, o => Assert.True(o.GapToLeaderSeconds >= 0));
        }
    }

    [Fact]
    public void Final_snapshot_order_matches_the_classification()
    {
        var telemetry = RunReal(seed: 3);
        var lastLap = telemetry.Laps[^1].Order.Select(o => o.CompetitorId);
        var classification = telemetry.Final.Entries.Select(e => e.CompetitorId);
        Assert.Equal(classification, lastLap);
    }

    [Fact]
    public void Telemetry_run_is_deterministic()
    {
        var a = RunReal(seed: 42);
        var b = RunReal(seed: 42);
        Assert.Equal(a.Winner(), b.Winner());
        Assert.Equal(
            a.Laps.Select(s => (s.Lap, s.LeaderId)),
            b.Laps.Select(s => (s.Lap, s.LeaderId)));
    }

    [Fact]
    public void Plain_run_and_telemetry_run_agree_on_the_result()
    {
        var carset = LoadCarset();
        var competitors = EntryList.BuildFromCarset(carset);
        var circuit = carset.Circuits[0];

        var grid = QualifyingSimulator.Run(competitors, circuit, carset.Coefficients, carset.Rules, new SeededRandom(9));
        var plain = new RaceSimulator().Run(competitors, grid, circuit, carset.Coefficients, carset.Rules, new SeededRandom(77));
        var telemetry = new RaceSimulator().RunWithTelemetry(competitors, grid, circuit, carset.Coefficients, carset.Rules, new SeededRandom(77));

        Assert.Equal(plain.Winner, telemetry.Final.Winner);
        Assert.Equal(plain.Entries.Select(e => e.CompetitorId), telemetry.Final.Entries.Select(e => e.CompetitorId));
    }
}

internal static class RaceTelemetryExtensions
{
    public static string Winner(this RaceTelemetry t) => t.Final.Winner;
}
