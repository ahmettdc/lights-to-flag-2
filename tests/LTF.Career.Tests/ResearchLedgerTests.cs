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

    private static ResearchState ProjectState(int correlation = 100, int retries = 0) => new()
    {
        ActiveProjects =
        [
            new DevelopmentProject
            {
                NodeId = "aero1", TargetAxis = CarAxis.AeroLowSpeed,
                EstimatedGainMin = 10, EstimatedGainMax = 10, CorrelationPercent = correlation, RetriesLeft = retries,
            },
        ],
    };

    [Fact]
    public void An_approved_project_raises_the_car_rating()
    {
        var alpha = ResearchLedger.DevelopSeason(BaseCarset(ProjectState()), 7).Carset.Teams.Single(t => t.Id == "alpha");

        Assert.Equal(60, alpha.Car.Aerodynamics.Value);         // 50 + gain 10
        Assert.Contains("aero1", alpha.Research.UnlockedNodeIds);
        Assert.Empty(alpha.Research.ActiveProjects);            // approved → cleared
    }

    [Fact]
    public void Approving_a_node_is_reported()
    {
        var dev = ResearchLedger.DevelopSeason(BaseCarset(ProjectState()), 7)
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
        var carset = BaseCarset(ProjectState());

        Assert.Equal(Key(ResearchLedger.DevelopSeason(carset, 7)), Key(ResearchLedger.DevelopSeason(carset, 7)));
    }

    [Fact]
    public void A_project_that_fails_validation_is_reworked_not_applied()
    {
        // Zero correlation → nothing realises, so the gain never clears the approval bar.
        var alpha = ResearchLedger.DevelopSeason(BaseCarset(ProjectState(correlation: 0, retries: 2)), 7)
            .Carset.Teams.Single(t => t.Id == "alpha");

        Assert.Equal(50, alpha.Car.Aerodynamics.Value); // unchanged — not approved
        var project = alpha.Research.ActiveProjects.Single();
        Assert.Equal(ValidationState.InManufacture, project.State); // sent back to rework
        Assert.Equal(1, project.RetriesLeft);                       // one retry consumed
    }

    [Fact]
    public void A_project_out_of_retries_is_abandoned()
    {
        var outcome = ResearchLedger.DevelopSeason(BaseCarset(ProjectState(correlation: 0, retries: 0)), 7);
        var alpha = outcome.Carset.Teams.Single(t => t.Id == "alpha");

        Assert.Equal(50, alpha.Car.Aerodynamics.Value);      // unchanged
        Assert.Empty(alpha.Research.ActiveProjects);         // dropped, not restarted this season
        Assert.Equal(1, outcome.Developments.Single(d => d.TeamId == "alpha").NodesAbandoned);
    }

    [Fact]
    public void Regulation_readiness_accrues_when_the_series_funds_it()
    {
        var carset = BaseCarset(ProjectState());
        carset = carset with
        {
            Rules = carset.Rules with { Research = carset.Rules.Research with { ReadinessGainPerSeason = 20 } },
        };

        var alpha = ResearchLedger.DevelopSeason(carset, 7).Carset.Teams.Single(t => t.Id == "alpha");

        Assert.Equal(20, alpha.Research.RegulationReadiness);
    }

    private static string Key(ResearchOutcome outcome) =>
        string.Join(";", outcome.Carset.Teams.Select(t => $"{t.Id}:{t.Car.Aerodynamics.Value},{t.Finances.Balance}"));
}
