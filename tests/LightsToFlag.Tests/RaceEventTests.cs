using System.Linq;
using LightsToFlag.Core.Data;
using LightsToFlag.Core.Domain;
using LightsToFlag.Core.Simulation;

namespace LightsToFlag.Tests;

public class RaceEventTests
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
    public void Race_produces_start_and_finish_events()
    {
        var telemetry = RunReal(seed: 1);
        var first = telemetry.Laps[0];
        var last = telemetry.Laps[^1];

        Assert.Contains(first.Events, e => e.Kind == RaceEventKind.Start);
        Assert.Contains(last.Events, e => e.Kind == RaceEventKind.Finish);

        var finish = last.Events.First(e => e.Kind == RaceEventKind.Finish);
        Assert.Equal(telemetry.Final.Winner, finish.PrimaryId);
    }

    [Fact]
    public void Race_produces_a_variety_of_events()
    {
        var events = RunReal(seed: 5).Laps.SelectMany(l => l.Events).ToList();

        Assert.NotEmpty(events);
        Assert.Contains(events, e => e.Kind == RaceEventKind.FastestLap);
        Assert.Contains(events, e => e.Kind == RaceEventKind.Overtake);
    }

    [Fact]
    public void Overtake_events_name_both_drivers()
    {
        var overtakes = RunReal(seed: 7).Laps.SelectMany(l => l.Events)
            .Where(e => e.Kind == RaceEventKind.Overtake);
        Assert.All(overtakes, e =>
        {
            Assert.False(string.IsNullOrEmpty(e.PrimaryId));
            Assert.False(string.IsNullOrEmpty(e.SecondaryId));
            Assert.NotEqual(e.PrimaryId, e.SecondaryId);
        });
    }

    [Fact]
    public void Retirement_events_carry_a_reason_and_match_the_classification()
    {
        var telemetry = RunReal(seed: 5);
        var retirementEvents = telemetry.Laps.SelectMany(l => l.Events)
            .Where(e => e.Kind == RaceEventKind.Retirement)
            .ToList();

        Assert.All(retirementEvents, e => Assert.False(string.IsNullOrEmpty(e.Note)));

        // One retirement event per car that actually retired.
        var retiredInResult = telemetry.Final.Entries.Count(e => e.Status == FinishStatus.Retired);
        Assert.Equal(retiredInResult, retirementEvents.Count);
    }

    [Fact]
    public void Events_are_deterministic_for_a_seed()
    {
        static string Key(RaceTelemetry t) => string.Join("|",
            t.Laps.SelectMany(l => l.Events).Select(e => $"{e.Lap}:{e.Kind}:{e.PrimaryId}:{e.SecondaryId}"));

        Assert.Equal(Key(RunReal(42)), Key(RunReal(42)));
    }

    [Fact]
    public void Plain_run_without_telemetry_still_works()
    {
        // BuildLapEvents only runs when telemetry is recorded; the plain path must be unaffected.
        var carset = LoadCarset();
        var competitors = EntryList.BuildFromCarset(carset);
        var circuit = carset.Circuits[0];
        var rng = new SeededRandom(3);
        var grid = QualifyingSimulator.Run(competitors, circuit, carset.Coefficients, carset.Rules, rng);
        var result = new RaceSimulator().Run(competitors, grid, circuit, carset.Coefficients, carset.Rules, rng);
        Assert.Equal(carset.Drivers.Count, result.Entries.Count);
    }
}
