using System.Linq;
using LTF.Domain;
using LTF.Domain.Common;
using LTF.Domain.Management;
using LTF.Domain.Racing;
using LTF.Domain.Rnd;
using Xunit;

namespace LTF.Career.Tests;

/// <summary>
/// FIA development freezes (Ri3): a regulation can freeze a car axis's R&amp;D <see cref="DevelopmentFreezeMode.Full"/>
/// (no development at all) or <see cref="DevelopmentFreezeMode.InSeasonOnly"/> (frozen mid-season, but developed
/// in one winter pulse at the boundary). The gate lives in <see cref="ResearchLedger"/> and is keyed on
/// <see cref="CarAxis"/>. With no freeze authored every axis develops freely and the carset is byte-identical.
/// </summary>
public class DevelopmentFreezeTests
{
    private static TechNode Aero1 => new()
    {
        Id = "aero1", DepartmentId = "aero", Category = CarAxis.AeroLowSpeed,
        Cost = 5_000_000, GainMin = 10, GainMax = 10, Correlation = 100,
    };

    // Alpha runs one aero node; the rate clears the whole pipeline in a season, so a seeded project reaches
    // approval at once. Alpha's aero starts at 50 so the +10 gain can't hit the clamp.
    private static Carset BaseCarset()
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
        var research = new ResearchState
        {
            ActiveProjects =
            [
                new DevelopmentProject
                {
                    NodeId = "aero1", TargetAxis = CarAxis.AeroLowSpeed,
                    EstimatedGainMin = 10, EstimatedGainMax = 10, CorrelationPercent = 100, RetriesLeft = 0,
                },
            ],
        };
        var teams = carset.Teams
            .Select(t => t.Id == "alpha"
                ? t with
                {
                    Car = t.Car with { Aerodynamics = new Rating(50) },
                    Finances = new Finances { Balance = 100_000_000 },
                    Research = research,
                }
                : t)
            .ToList();
        return carset with { TechTree = tree, Rules = rules, Teams = teams };
    }

    private static Carset WithFreeze(Carset carset, CarAxis axis, DevelopmentFreezeMode mode) =>
        carset with
        {
            Regulations = carset.Regulations with { DevelopmentFreezes = [new AxisFreeze { Axis = axis, Mode = mode }] },
        };

    private static int Aero(Carset carset) => carset.Teams.Single(t => t.Id == "alpha").Car.Aerodynamics.Value;

    // A full season of per-round development steps (mid-season path).
    private static Carset FullSeason(Carset carset)
    {
        var current = carset;
        for (var i = 0; i < 4; i++)
        {
            current = ResearchLedger.DevelopStep(current, 7, roundIndex: i, roundCount: 4).Carset;
        }

        return current;
    }

    [Fact]
    public void An_unrestricted_axis_develops_mid_season()
    {
        var developed = FullSeason(BaseCarset()); // no freeze
        Assert.True(Aero(developed) > 50);
    }

    [Fact]
    public void A_full_freeze_stops_the_axis_developing_all_season_and_over_the_winter()
    {
        var carset = WithFreeze(BaseCarset(), CarAxis.AeroLowSpeed, DevelopmentFreezeMode.Full);

        var midSeason = FullSeason(carset);
        Assert.Equal(50, Aero(midSeason)); // frozen mid-season

        var afterWinter = ResearchLedger.DevelopWinter(midSeason, 7).Carset;
        Assert.Equal(50, Aero(afterWinter)); // and no winter catch-up either — fully frozen
    }

    [Fact]
    public void An_in_season_freeze_holds_the_axis_mid_season_but_the_winter_pulse_develops_it()
    {
        var carset = WithFreeze(BaseCarset(), CarAxis.AeroLowSpeed, DevelopmentFreezeMode.InSeasonOnly);

        var midSeason = FullSeason(carset);
        Assert.Equal(50, Aero(midSeason)); // frozen during the season

        var afterWinter = ResearchLedger.DevelopWinter(midSeason, 7).Carset;
        Assert.True(Aero(afterWinter) > 50); // develops over the winter
    }

    [Fact]
    public void A_freeze_on_another_axis_leaves_this_axis_untouched()
    {
        var frozenElsewhere = WithFreeze(BaseCarset(), CarAxis.PowerUnit, DevelopmentFreezeMode.Full);
        var developed = FullSeason(frozenElsewhere);
        Assert.True(Aero(developed) > 50); // only PowerUnit is frozen; the aero project still develops
    }

    [Fact]
    public void The_winter_pulse_is_inert_without_an_in_season_freeze()
    {
        var noFreeze = BaseCarset();
        Assert.Same(noFreeze, ResearchLedger.DevelopWinter(noFreeze, 7).Carset);

        var fullFrozen = WithFreeze(noFreeze, CarAxis.AeroLowSpeed, DevelopmentFreezeMode.Full);
        Assert.Same(fullFrozen, ResearchLedger.DevelopWinter(fullFrozen, 7).Carset); // Full has no winter catch-up
    }

    [Fact]
    public void The_winter_pulse_is_deterministic()
    {
        var carset = WithFreeze(BaseCarset(), CarAxis.AeroLowSpeed, DevelopmentFreezeMode.InSeasonOnly);
        Assert.Equal(
            Aero(ResearchLedger.DevelopWinter(carset, 7).Carset),
            Aero(ResearchLedger.DevelopWinter(carset, 7).Carset));
    }
}
