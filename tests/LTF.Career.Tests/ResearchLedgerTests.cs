using System.Linq;
using LTF.Domain;
using LTF.Domain.Common;
using LTF.Domain.Management;
using LTF.Domain.Racing;
using LTF.Domain.Rnd;
using Xunit;

namespace LTF.Career.Tests;

public class ResearchLedgerTests
{
    private static TechNode Aero1 => new()
    {
        Id = "aero1", DepartmentId = "aero", Category = CarAxis.AeroLowSpeed,
        Cost = 5_000_000, GainMin = 10, GainMax = 10, Correlation = 100,
    };

    // Alpha runs one aero node; the rate clears the whole pipeline in a season, so a seeded project
    // reaches approval at once. Alpha's aero starts at 50 so the +10 gain can't hit the clamp.
    private static Carset BaseCarset(ResearchState alphaResearch)
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 4);
        var tree = new TechTree
        {
            Departments = [new Department { Id = "aero", Name = "Aero" }],
            Nodes = [Aero1],
        };
        var rules = carset.Rules with
        {
            Research = new ResearchRules { BaseProgressPerSeason = 1000, StepProgress = 100, BaseActiveProjects = 1 },
        };
        var teams = carset.Teams
            .Select(t => t.Id == "alpha"
                ? t with
                {
                    Car = t.Car with { Aerodynamics = new Rating(50) },
                    Finances = new Finances { Balance = 100_000_000 },
                    Research = alphaResearch,
                }
                : t)
            .ToList();
        return carset with { TechTree = tree, Rules = rules, Teams = teams };
    }

    private static ResearchState WithProject => new()
    {
        ActiveProjects =
        [
            new DevelopmentProject
            {
                NodeId = "aero1", TargetAxis = CarAxis.AeroLowSpeed,
                EstimatedGainMin = 10, EstimatedGainMax = 10, CorrelationPercent = 100,
            },
        ],
    };

    [Fact]
    public void An_approved_project_raises_the_car_rating()
    {
        var alpha = ResearchLedger.DevelopSeason(BaseCarset(WithProject), 7).Carset.Teams.Single(t => t.Id == "alpha");

        Assert.Equal(60, alpha.Car.Aerodynamics.Value);         // 50 + gain 10
        Assert.Contains("aero1", alpha.Research.UnlockedNodeIds);
        Assert.Empty(alpha.Research.ActiveProjects);            // approved → cleared
    }

    [Fact]
    public void Approving_a_node_is_reported()
    {
        var dev = ResearchLedger.DevelopSeason(BaseCarset(WithProject), 7)
            .Developments.Single(d => d.TeamId == "alpha");

        Assert.Equal(1, dev.NodesApproved);
        Assert.Contains("aero1", dev.ApprovedNodeIds);
    }

    [Fact]
    public void A_free_slot_auto_starts_an_affordable_node_and_charges_it()
    {
        var alpha = ResearchLedger.DevelopSeason(BaseCarset(ResearchState.Empty), 7)
            .Carset.Teams.Single(t => t.Id == "alpha");

        var project = alpha.Research.ActiveProjects.Single();
        Assert.Equal("aero1", project.NodeId);
        Assert.Equal(ValidationState.InDesign, project.State);  // started this season, advances next
        Assert.Equal(95_000_000L, alpha.Finances.Balance);      // 100M − 5M node cost
    }

    [Fact]
    public void A_carset_without_a_tech_tree_is_inert()
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 4);

        var outcome = ResearchLedger.DevelopSeason(carset, 7);

        Assert.Same(carset, outcome.Carset); // untouched reference
        Assert.Empty(outcome.Developments);
    }

    [Fact]
    public void Development_is_deterministic()
    {
        var carset = BaseCarset(WithProject);

        Assert.Equal(Key(ResearchLedger.DevelopSeason(carset, 7)), Key(ResearchLedger.DevelopSeason(carset, 7)));
    }

    private static string Key(ResearchOutcome outcome) =>
        string.Join(";", outcome.Carset.Teams.Select(t => $"{t.Id}:{t.Car.Aerodynamics.Value},{t.Finances.Balance}"));
}
