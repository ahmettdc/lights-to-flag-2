using System.Collections.Generic;
using System.Linq;
using LTF.Domain.Common;
using LTF.Simulation.Practice;
using LTF.Simulation.Qualifying;
using LTF.Simulation.Racing;
using Xunit;

namespace LTF.Simulation.Tests;

public class PracticeSimulatorTests
{
    private static IReadOnlyDictionary<string, PracticeProgram> AllOn(
        IReadOnlyList<Competitor> grid, PracticeProgram program) =>
        grid.ToDictionary(c => c.Id, _ => program, StringComparer.Ordinal);

    [Fact]
    public void Practice_is_deterministic()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);
        var programs = AllOn(grid, PracticeProgram.SetupWork);

        var a = PracticeSimulator.Run(grid, programs, SimFixtures.Balance, 7);
        var b = PracticeSimulator.Run(grid, programs, SimFixtures.Balance, 7);

        Assert.Equal(
            a.Entries.Select(e => (e.CompetitorId, e.DataQuality, e.Setup.QualifyingBonusSeconds, e.Setup.ErrorFactor)),
            b.Entries.Select(e => (e.CompetitorId, e.DataQuality, e.Setup.QualifyingBonusSeconds, e.Setup.ErrorFactor)));
    }

    [Fact]
    public void Setup_work_gives_the_biggest_qualifying_gain()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);

        var setup = PracticeSimulator.Run(grid, AllOn(grid, PracticeProgram.SetupWork), SimFixtures.Balance, 7);
        var raceSim = PracticeSimulator.Run(grid, AllOn(grid, PracticeProgram.RaceSimulation), SimFixtures.Balance, 7);

        // Same seed and streams, so data quality matches — only the program's weighting differs.
        foreach (var c in grid)
        {
            Assert.True(setup.Setups[c.Id].QualifyingBonusSeconds > raceSim.Setups[c.Id].QualifyingBonusSeconds);
        }
    }

    [Fact]
    public void Race_simulation_practice_cuts_the_error_chance()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);

        var raceSim = PracticeSimulator.Run(grid, AllOn(grid, PracticeProgram.RaceSimulation), SimFixtures.Balance, 7);
        var setupWork = PracticeSimulator.Run(grid, AllOn(grid, PracticeProgram.SetupWork), SimFixtures.Balance, 7);

        foreach (var c in grid)
        {
            Assert.True(raceSim.Setups[c.Id].ErrorFactor < 1.0);
            Assert.Equal(1.0, setupWork.Setups[c.Id].ErrorFactor);
        }
    }

    [Fact]
    public void A_practice_setup_lowers_a_qualifying_lap()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);
        var rules = carset.Rules with { Qualifying = QualifyingFormat.SingleLap };
        var practice = PracticeSimulator.Run(grid, AllOn(grid, PracticeProgram.SetupWork), SimFixtures.Balance, 7);

        var plain = QualifyingSimulator.Run(carset.Circuits[0], grid, rules, SimFixtures.Balance, 7);
        var withPractice = QualifyingSimulator.Run(carset.Circuits[0], grid, rules, SimFixtures.Balance, 7, practice.Setups);

        var plainById = plain.Grid.ToDictionary(e => e.CompetitorId, e => e.BestLap);
        foreach (var e in withPractice.Grid)
        {
            // Same seed and lap draws — only the practice gain is subtracted, so it's strictly quicker.
            Assert.True(e.BestLap < plainById[e.CompetitorId],
                $"{e.CompetitorId}: practice {e.BestLap} !< plain {plainById[e.CompetitorId]}");
        }
    }

    [Fact]
    public void Empty_practice_leaves_qualifying_identical()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);
        var empty = new Dictionary<string, PracticeSetup>();

        var withoutArg = QualifyingSimulator.Run(carset.Circuits[0], grid, carset.Rules, SimFixtures.Balance, 7);
        var withEmpty = QualifyingSimulator.Run(carset.Circuits[0], grid, carset.Rules, SimFixtures.Balance, 7, empty);

        Assert.Equal(
            withoutArg.Grid.Select(e => (e.GridPosition, e.CompetitorId, e.Part, e.BestLap)),
            withEmpty.Grid.Select(e => (e.GridPosition, e.CompetitorId, e.Part, e.BestLap)));
    }

    [Fact]
    public void A_practice_race_bonus_makes_a_car_faster()
    {
        var carset = SimFixtures.Carset();
        var grid = EntryList.Build(carset);
        var target = grid[0].Id;

        // Calm balance (no incidents, traffic off) isolates the single car's per-lap gain.
        var setups = new Dictionary<string, PracticeSetup>(StringComparer.Ordinal)
        {
            [target] = new PracticeSetup { RaceBonusSeconds = 0.2 },
        };

        var plain = RaceSimulator.Run(carset.Circuits[0], grid, carset.Rules, SimFixtures.CalmBalance, 7);
        var boosted = RaceSimulator.Run(carset.Circuits[0], grid, carset.Rules, SimFixtures.CalmBalance, 7, setups: setups);

        static double TimeOf(RaceResult r, string id) => r.Classification.Single(e => e.CompetitorId == id).TotalTime;
        Assert.True(TimeOf(boosted, target) < TimeOf(plain, target));
    }
}
