using System.IO;
using System.Linq;
using LightsToFlag.Core.Career;
using LightsToFlag.Core.Data;
using LightsToFlag.Core.Domain;
using LightsToFlag.Core.Saves;

namespace LightsToFlag.Tests;

public class CareerTests
{
    private static Carset LoadCarset() =>
        new LegacyTextCarsetLoader().Load(TestCarsets.Path(TestCarsets.F1_2019));

    [Fact]
    public void Simulated_season_produces_full_standings_with_a_champion()
    {
        var carset = LoadCarset();
        var engine = new CareerEngine();
        var state = engine.SimulateWholeSeason(engine.Start(carset, playerId: "D1", seed: 2024), carset);

        Assert.True(engine.IsSeasonComplete(state, carset));
        Assert.Equal(carset.Circuits.Count, state.CompletedRounds.Count);

        var standings = engine.DriverStandings(state, carset);
        Assert.Equal(carset.Drivers.Count, standings.Count);
        Assert.Equal(Enumerable.Range(1, standings.Count), standings.Select(s => s.Position));

        // The champion has the most points and it is a real, non-negative total.
        Assert.True(standings[0].Points >= standings[^1].Points);
        Assert.True(standings[0].Points > 0);
    }

    [Fact]
    public void Driver_points_reconcile_with_round_results()
    {
        var carset = LoadCarset();
        var engine = new CareerEngine();
        var state = engine.SimulateWholeSeason(engine.Start(carset, "D1", 11), carset);

        var totalFromRounds = state.CompletedRounds.SelectMany(r => r.Entries).Sum(e => e.Points);
        var totalFromStandings = engine.DriverStandings(state, carset).Sum(s => s.Points);
        Assert.Equal(totalFromRounds, totalFromStandings, precision: 6);
    }

    [Fact]
    public void Same_seed_reproduces_the_champion()
    {
        var carset = LoadCarset();
        var engine = new CareerEngine();

        var a = engine.SimulateWholeSeason(engine.Start(carset, "D1", 7), carset);
        var b = engine.SimulateWholeSeason(engine.Start(carset, "D1", 7), carset);

        Assert.Equal(engine.DriverStandings(a, carset)[0].CompetitorId,
                     engine.DriverStandings(b, carset)[0].CompetitorId);
    }

    [Fact]
    public void Advancing_a_season_ages_drivers_and_generates_offers()
    {
        var carset = LoadCarset();
        var engine = new CareerEngine();
        var start = engine.Start(carset, playerId: "D1", seed: 1);
        var youngestBefore = start.Entrants.Min(e => e.Driver.Age);

        var next = engine.AdvanceToNextSeason(engine.SimulateWholeSeason(start, carset), carset);

        Assert.Equal(1, next.SeasonIndex);
        Assert.Empty(next.CompletedRounds);
        Assert.Single(next.History);
        Assert.NotEmpty(next.PendingOffers);
        // Everyone who kept their seat is a year older; the field size is stable.
        Assert.Equal(start.Entrants.Count, next.Entrants.Count);
        Assert.True(next.Entrants.Min(e => e.Driver.Age) >= youngestBefore);
    }

    [Fact]
    public void Save_and_load_round_trips_the_career()
    {
        var carset = LoadCarset();
        var engine = new CareerEngine();
        var state = engine.SimulateWholeSeason(engine.Start(carset, "D1", 55), carset);

        var root = Path.Combine(Path.GetTempPath(), "ltf-tests", Path.GetRandomFileName());
        try
        {
            var store = new FileSaveStore(root);
            store.Save("career1", state);
            Assert.True(store.Exists("career1"));
            Assert.Contains("career1", store.ListSlots());

            var loaded = store.Load("career1");

            Assert.Equal(state.SeasonIndex, loaded.SeasonIndex);
            Assert.Equal(state.Seed, loaded.Seed);
            Assert.Equal(state.PlayerId, loaded.PlayerId);
            Assert.Equal(state.Entrants.Count, loaded.Entrants.Count);
            Assert.Equal(state.CompletedRounds.Count, loaded.CompletedRounds.Count);

            // Standings recomputed from the loaded state match the original.
            var before = engine.DriverStandings(state, carset).Select(s => (s.CompetitorId, s.Points));
            var after = engine.DriverStandings(loaded, carset).Select(s => (s.CompetitorId, s.Points));
            Assert.Equal(before, after);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void Serialized_save_is_readable_json()
    {
        var carset = LoadCarset();
        var engine = new CareerEngine();
        var state = engine.Start(carset, "D1", 1);

        var json = CareerSaveSerializer.Serialize(state);
        Assert.Contains("\"carsetName\"", json);
        Assert.Contains("F1 2019", json);

        var back = CareerSaveSerializer.Deserialize(json);
        Assert.Equal(state.CarsetName, back.CarsetName);
        Assert.Equal(state.Entrants.Count, back.Entrants.Count);
    }
}
