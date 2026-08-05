using System.Linq;
using LTF.Simulation.Sweep;
using Xunit;

namespace LTF.Simulation.Tests;

public class BalanceSweepTests
{
    [Fact]
    public void A_sweep_is_deterministic()
    {
        var carset = SimFixtures.Carset();

        var a = BalanceSweep.Run(carset, seasons: 4, seed: 2024);
        var b = BalanceSweep.Run(carset, seasons: 4, seed: 2024);

        Assert.Equal(Key(a), Key(b));
    }

    [Fact]
    public void A_sweep_covers_every_race()
    {
        var carset = SimFixtures.Carset();

        var report = BalanceSweep.Run(carset, seasons: 3, seed: 7);

        Assert.Equal(3 * carset.Calendar.Count, report.Races);
        // Every competitor is classified in every race, so each one's Starts equals the race count.
        Assert.All(report.Competitors, c => Assert.Equal(report.Races, c.Starts));
    }

    [Fact]
    public void Titles_sum_to_the_season_count()
    {
        var carset = SimFixtures.Carset();

        var report = BalanceSweep.Run(carset, seasons: 6, seed: 7);

        Assert.Equal(6, report.Competitors.Sum(c => c.Titles));
    }

    [Fact]
    public void A_faster_field_wins_more()
    {
        // Fixture: team alpha (car 85 — drivers d1/d2) is clearly faster than bravo (70 — d3/d4).
        var carset = SimFixtures.Carset();

        var report = BalanceSweep.Run(carset, seasons: 10, seed: 7);

        int PointsOf(params string[] ids) =>
            report.Competitors.Where(c => ids.Contains(c.CompetitorId)).Sum(c => c.Points);

        Assert.True(PointsOf("d1", "d2") > PointsOf("d3", "d4"),
            $"alpha {PointsOf("d1", "d2")} vs bravo {PointsOf("d3", "d4")}");
        Assert.Contains(report.Competitors[0].CompetitorId, new[] { "d1", "d2" });
    }

    [Fact]
    public void Sweep_rates_are_in_a_sane_band()
    {
        var carset = SimFixtures.Carset();

        var report = BalanceSweep.Run(carset, seasons: 5, seed: 7);

        Assert.InRange(report.RetirementRate, 0.0, 1.0);
        Assert.True(report.SafetyCarsPerRace >= 0.0);
        // The fixture mandates no pit stops, so the average is exactly zero.
        Assert.Equal(0.0, report.AveragePitStopsPerCar);
    }

    private static string Key(SweepReport r) =>
        $"{r.Seasons}/{r.Rounds}/{r.Races}/{r.RetirementRate:R}/{r.SafetyCarsPerRace:R}/{r.AveragePitStopsPerCar:R}|" +
        string.Join(";", r.Competitors.Select(c =>
            $"{c.CompetitorId},{c.Titles},{c.Wins},{c.Points},{c.Starts},{c.Retirements}"));
}
