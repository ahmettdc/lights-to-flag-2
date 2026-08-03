using System.Collections.Generic;
using System.Linq;
using LightsToFlag.Core.Data;
using LightsToFlag.Core.Domain;
using LightsToFlag.Core.Simulation;

namespace LightsToFlag.Tests;

public class RaceTests
{
    private static Carset LoadCarset() =>
        new LegacyTextCarsetLoader().Load(TestCarsets.Path(TestCarsets.F1_2019));

    private static RaceClassification RunRealRace(int seed)
    {
        var carset = LoadCarset();
        var competitors = EntryList.BuildFromCarset(carset);
        var circuit = carset.Circuits[0];
        var rng = new SeededRandom(seed);
        var grid = QualifyingSimulator.Run(competitors, circuit, carset.Coefficients, carset.Rules, rng);
        return new RaceSimulator().Run(competitors, grid, circuit, carset.Coefficients, carset.Rules, rng);
    }

    [Fact]
    public void Real_race_classifies_every_car_with_unique_positions()
    {
        var carset = LoadCarset();
        var result = RunRealRace(seed: 1);

        Assert.Equal(carset.Drivers.Count, result.Entries.Count);
        Assert.Equal(Enumerable.Range(1, result.Entries.Count), result.Entries.Select(e => e.Position));
        Assert.Equal(result.Entries.Count, result.Entries.Select(e => e.CompetitorId).Distinct().Count());
    }

    [Fact]
    public void Race_invariants_hold()
    {
        var result = RunRealRace(seed: 5);
        foreach (var e in result.Entries)
        {
            Assert.True(e.TotalTimeSeconds > 0, "total time must be positive");
            Assert.InRange(e.LapsCompleted, 0, result.TotalLaps);
            Assert.True(e.Points >= 0);
        }

        // Exactly one fastest lap is flagged.
        Assert.Equal(1, result.Entries.Count(e => e.FastestLap));
    }

    [Fact]
    public void Same_seed_reproduces_the_result()
    {
        var a = RunRealRace(seed: 99);
        var b = RunRealRace(seed: 99);
        Assert.Equal(a.Entries.Select(e => e.CompetitorId), b.Entries.Select(e => e.CompetitorId));
        Assert.Equal(a.Winner, b.Winner);
    }

    [Fact]
    public void Winner_scores_the_top_points()
    {
        var result = RunRealRace(seed: 3);
        var winner = result.Entries[0];
        Assert.Equal(FinishStatus.Finished, winner.Status);
        // 25 for the win, plus a possible fastest-lap point.
        Assert.True(winner.Points >= 25);
    }

    [Fact]
    public void Points_awarded_never_exceed_the_scheme_total()
    {
        var carset = LoadCarset();
        var result = RunRealRace(seed: 7);
        var maxPossible = carset.Rules.PrimaryPoints.Sum() + carset.Rules.PointsForFastestLap;
        Assert.True(result.Entries.Sum(e => e.Points) <= maxPossible + 1e-9);
    }

    [Fact]
    public void A_dominant_car_wins_most_of_the_time()
    {
        var field = new List<Competitor>
        {
            Fixtures.Competitor("1",
                Fixtures.Driver("Ace", pace: 10, consistency: 10, concentration: 10),
                Fixtures.Team("Top", aero: 10, mech: 10, engine: 10, reliability: 10)),
        };
        for (var i = 2; i <= 10; i++)
        {
            field.Add(Fixtures.Competitor(i.ToString(),
                Fixtures.Driver($"D{i}", pace: 4, consistency: 4, concentration: 6),
                Fixtures.Team($"T{i}", aero: 4, mech: 4, engine: 4, reliability: 9)));
        }

        var circuit = Fixtures.Circuit(laps: 40);
        var rules = Fixtures.Rules();
        var coeff = Fixtures.Coefficients();

        var wins = 0;
        for (var seed = 0; seed < 20; seed++)
        {
            var rng = new SeededRandom(seed);
            var grid = QualifyingSimulator.Run(field, circuit, coeff, rules, rng);
            var result = new RaceSimulator().Run(field, grid, circuit, coeff, rules, rng);
            if (result.Winner == "1")
            {
                wins++;
            }
        }

        Assert.True(wins >= 16, $"dominant car won {wins}/20");
    }

    [Fact]
    public void Unreliable_cars_retire_more_often_than_reliable_ones()
    {
        var circuit = Fixtures.Circuit(laps: 50, attrition: 6);
        var rules = Fixtures.Rules();
        var coeff = Fixtures.Coefficients();

        int Dnfs(int reliability)
        {
            var total = 0;
            for (var seed = 0; seed < 15; seed++)
            {
                var field = Enumerable.Range(1, 10)
                    .Select(i => Fixtures.Competitor(i.ToString(),
                        Fixtures.Driver($"D{i}", concentration: 10),
                        Fixtures.Team($"T{i}", reliability: reliability)))
                    .ToList<Competitor>();
                var rng = new SeededRandom(seed);
                var grid = QualifyingSimulator.Run(field, circuit, coeff, rules, rng);
                var result = new RaceSimulator().Run(field, grid, circuit, coeff, rules, rng);
                total += result.Entries.Count(e => e.Status == FinishStatus.Retired);
            }

            return total;
        }

        Assert.True(Dnfs(2) > Dnfs(10), "fragile cars should retire more than bulletproof ones");
    }

    [Fact]
    public void Safety_car_race_still_produces_a_valid_classification()
    {
        var circuit = Fixtures.Circuit(laps: 40, scLikelihood: 10);
        var rules = Fixtures.Rules();
        var coeff = Fixtures.Coefficients();
        var field = Enumerable.Range(1, 12)
            .Select(i => Fixtures.Competitor(i.ToString()))
            .ToList<Competitor>();

        var rng = new SeededRandom(4);
        var grid = QualifyingSimulator.Run(field, circuit, coeff, rules, rng);
        var result = new RaceSimulator().Run(field, grid, circuit, coeff, rules, rng);

        Assert.Equal(12, result.Entries.Count);
        Assert.Equal(Enumerable.Range(1, 12), result.Entries.Select(e => e.Position));
    }
}
