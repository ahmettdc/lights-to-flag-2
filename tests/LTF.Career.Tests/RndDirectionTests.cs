using System.Linq;
using LTF.Domain;
using LTF.Domain.Management;
using LTF.Domain.Racing;
using LTF.Domain.Rnd;
using Xunit;

namespace LTF.Career.Tests;

public class RndDirectionTests
{
    // A one-slot team with a power node listed first and an aero node second, both affordable. Which one
    // gets started shows whether a concept steered the choice; catalog order alone would pick the power node.
    private static Carset TwoNodeCarset()
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 4);
        var tree = new TechTree
        {
            Departments = [new Department { Id = "pu", Name = "PU" }, new Department { Id = "aero", Name = "Aero" }],
            Nodes =
            [
                new TechNode { Id = "power1", DepartmentId = "pu", Category = CarAxis.PowerUnit, Cost = 5_000_000, GainMin = 10, GainMax = 10, Correlation = 100 },
                new TechNode { Id = "aero1", DepartmentId = "aero", Category = CarAxis.AeroLowSpeed, Cost = 5_000_000, GainMin = 10, GainMax = 10, Correlation = 100 },
            ],
        };
        var rules = carset.Rules with
        {
            Research = new ResearchRules { BaseProgressPerSeason = 1000, StepProgress = 100, BaseActiveProjects = 1 },
        };
        var teams = carset.Teams
            .Select(t => t.Id == "alpha" ? t with { Finances = new Finances { Balance = 100_000_000 } } : t)
            .ToList();
        return carset with { TechTree = tree, Rules = rules, Teams = teams };
    }

    [Fact]
    public void A_concept_directive_steers_which_node_is_started()
    {
        var carset = TwoNodeCarset();

        // No directive → catalog order picks the first-listed (power) node.
        var plain = ResearchLedger.DevelopSeason(carset, 7).Carset.Teams.Single(t => t.Id == "alpha");
        Assert.Equal("power1", plain.Research.ActiveProjects.Single().NodeId);

        // An aero-heavy directive reorders the aero node ahead, so it is started instead.
        var aero = new RndDirection("alpha", new ConceptDirection { AeroLean = 100 });
        var steered = ResearchLedger.DevelopSeason(carset, 7, aero).Carset.Teams.Single(t => t.Id == "alpha");
        Assert.Equal("aero1", steered.Research.ActiveProjects.Single().NodeId);
    }

    [Fact]
    public void A_budget_cap_curtails_development_spend()
    {
        var carset = TwoNodeCarset();

        // Uncapped: alpha starts (and pays for) a 5M node.
        var uncapped = ResearchLedger.DevelopSeason(carset, 7).Carset.Teams.Single(t => t.Id == "alpha");
        Assert.NotEmpty(uncapped.Research.ActiveProjects);
        Assert.Equal(95_000_000L, uncapped.Finances.Balance);

        // A cap below the node cost prices the node out: nothing is started, nothing spent.
        var capped = new RndDirection("alpha", ConceptDirection.Neutral, budgetCap: 1_000_000);
        var team = ResearchLedger.DevelopSeason(carset, 7, capped).Carset.Teams.Single(t => t.Id == "alpha");
        Assert.Empty(team.Research.ActiveProjects);
        Assert.Equal(100_000_000L, team.Finances.Balance);
    }

    [Fact]
    public void A_null_or_neutral_directive_develops_identically()
    {
        var carset = TwoNodeCarset();

        var baseline = Key(ResearchLedger.DevelopSeason(carset, 7));
        var explicitNull = Key(ResearchLedger.DevelopSeason(carset, 7, null));
        var neutral = Key(ResearchLedger.DevelopSeason(carset, 7, new RndDirection("alpha", ConceptDirection.Neutral)));

        Assert.Equal(baseline, explicitNull);
        Assert.Equal(baseline, neutral);
    }

    [Fact]
    public void A_directive_only_touches_the_team_it_names()
    {
        // Both teams can afford a node; an alpha-only directive must leave bravo's development unchanged.
        var baseCarset = TwoNodeCarset();
        var carset = baseCarset with
        {
            Teams = baseCarset.Teams.Select(t => t with { Finances = new Finances { Balance = 100_000_000 } }).ToList(),
        };

        var plainBravo = ResearchLedger.DevelopSeason(carset, 7).Carset.Teams.Single(t => t.Id == "bravo");
        var directed = new RndDirection("alpha", new ConceptDirection { AeroLean = 100 }, budgetCap: 0);
        var directedBravo = ResearchLedger.DevelopSeason(carset, 7, directed).Carset.Teams.Single(t => t.Id == "bravo");

        Assert.Equal(
            plainBravo.Research.ActiveProjects.Single().NodeId,
            directedBravo.Research.ActiveProjects.Single().NodeId);
    }

    [Fact]
    public void A_steered_development_is_deterministic()
    {
        var carset = TwoNodeCarset();
        var directive = new RndDirection("alpha", new ConceptDirection { AeroLean = 100 }, budgetCap: 50_000_000);

        Assert.Equal(
            Key(ResearchLedger.DevelopSeason(carset, 7, directive)),
            Key(ResearchLedger.DevelopSeason(carset, 7, directive)));
    }

    private static string Key(ResearchOutcome outcome) =>
        string.Join(";", outcome.Carset.Teams.Select(t =>
            $"{t.Id}:{t.Car.Aerodynamics.Value},{t.Car.PowerUnit.Value},{t.Finances.Balance}," +
            $"[{string.Join(",", t.Research.ActiveProjects.Select(p => p.NodeId))}]"));
}
