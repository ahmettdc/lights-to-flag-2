using System;
using System.Linq;
using LTF.Domain;
using LTF.Domain.Common;
using LTF.Domain.Management;
using LTF.Domain.Racing;
using LTF.Domain.Rnd;
using Xunit;

namespace LTF.Career.Tests;

public class ResearchSweepTests
{
    // A chain of aero nodes, each unlocking the next, every one worth a modest +5 aero on approval.
    private static TechTree ChainTree(int count)
    {
        var nodes = Enumerable.Range(1, count)
            .Select(i => new TechNode
            {
                Id = $"aero{i}", DepartmentId = "aero", Category = CarAxis.AeroLowSpeed,
                Cost = 1_000_000, GainMin = 5, GainMax = 5, Correlation = 100,
                Prerequisites = i == 1 ? Array.Empty<string>() : new[] { $"aero{i - 1}" },
            })
            .ToList();
        return new TechTree { Departments = [new Department { Id = "aero", Name = "Aero" }], Nodes = nodes };
    }

    // Both teams develop fast: the rate clears the whole pipeline in a season, so a node approves each year.
    private static Carset DevelopingCarset()
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 4);
        var rules = carset.Rules with
        {
            Research = new ResearchRules { BaseProgressPerSeason = 1000, StepProgress = 100, BaseActiveProjects = 1 },
        };
        var teams = carset.Teams
            .Select(t => t with { Car = Flat(50), Finances = new Finances { Balance = 200_000_000 } })
            .ToList();
        return carset with { TechTree = ChainTree(4), Rules = rules, Teams = teams };
    }

    // Equal starting cars, but alpha's facilities are maxed and bravo's are bare; a facility-weighted rate
    // means alpha walks the pipeline twice as fast and so develops further over the same seasons.
    private static Carset DifferentiatedCarset()
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 4);
        var rules = carset.Rules with
        {
            Research = new ResearchRules
            {
                BaseProgressPerSeason = 100, StepProgress = 100, BaseActiveProjects = 1, FacilityWeight = 1.0,
            },
        };
        var teams = carset.Teams
            .Select(t => t with
            {
                Car = Flat(50),
                Finances = new Finances { Balance = 200_000_000 },
                Facilities = t.Id == "alpha" ? AllLevel(5) : AllLevel(1),
            })
            .ToList();
        return carset with { TechTree = ChainTree(6), Rules = rules, Teams = teams };
    }

    [Fact]
    public void A_developing_team_measurably_raises_its_car_within_the_ceiling()
    {
        var report = ResearchSweep.Run(DevelopingCarset(), seasons: 4, seed: 7);

        Assert.True(report.MaxOverallGain > 0);                       // R&D actually moved a car
        Assert.True(report.TotalNodesApproved > 0);
        Assert.All(report.Teams, t => Assert.True(t.FinalOverall > t.StartOverall));
        Assert.All(report.Teams, t => Assert.True(t.MaxOverall <= 100)); // bounded — no runaway past the ceiling
    }

    [Fact]
    public void A_better_equipped_team_develops_further()
    {
        var report = ResearchSweep.Run(DifferentiatedCarset(), seasons: 8, seed: 7);

        var alpha = report.Teams.Single(t => t.TeamId == "alpha");
        var bravo = report.Teams.Single(t => t.TeamId == "bravo");

        Assert.True(alpha.NodesApproved > bravo.NodesApproved); // maxed facilities → faster development
        Assert.True(alpha.FinalOverall > bravo.FinalOverall);
    }

    [Fact]
    public void A_research_free_sweep_moves_no_car()
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 4); // no tech tree, no research rules

        var report = ResearchSweep.Run(carset, seasons: 3, seed: 7);

        Assert.Equal(0, report.TotalNodesApproved);
        Assert.Equal(0, report.MaxOverallGain);
        Assert.All(report.Teams, t => Assert.Equal(t.StartOverall, t.FinalOverall));
    }

    [Fact]
    public void The_sweep_is_deterministic()
    {
        var carset = DevelopingCarset();

        Assert.Equal(Key(ResearchSweep.Run(carset, 4, 7)), Key(ResearchSweep.Run(carset, 4, 7)));
    }

    [Fact]
    public void In_season_progression_develops_the_car_during_the_season()
    {
        var progress = SeasonSimulator.RunProgressed(DevelopingCarset(), 7, new RndProgression(7));

        Assert.Contains(progress.Carset.Teams, t => t.Car.Aerodynamics.Value > 50); // approved mid-season
        Assert.All(progress.Carset.Teams, t => Assert.True(t.Car.Aerodynamics.Value <= 100));
    }

    [Fact]
    public void A_research_free_progression_leaves_the_carset_untouched()
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 4);

        var progress = SeasonSimulator.RunProgressed(carset, 7, new RndProgression(7));

        Assert.Same(carset, progress.Carset); // no tech tree → the progression is a no-op
    }

    private static string Key(ResearchSweepReport report) =>
        string.Join(";", report.Teams.Select(t =>
            $"{t.TeamId}:{t.StartOverall},{t.FinalOverall},{t.MaxOverall},{t.NodesUnlocked},{t.NodesApproved},{t.NodesAbandoned}"));

    private static Car Flat(int v) => new()
    {
        Aerodynamics = new(v), Chassis = new(v), PowerUnit = new(v), TyreGentleness = new(v), Reliability = new(v),
    };

    private static Facilities AllLevel(int n) => new()
    {
        DesignOffice = new(n), WindTunnel = new(n), Cfd = new(n), CompositeManufacturing = new(n),
        MechanicalWorkshop = new(n), QualityControl = new(n), Simulator = new(n), Dyno = new(n),
        PitCrewCentre = new(n), DataCentre = new(n),
    };
}
