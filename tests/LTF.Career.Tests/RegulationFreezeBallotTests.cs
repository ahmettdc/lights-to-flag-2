using System.Linq;
using LTF.Domain;
using LTF.Domain.Management;
using LTF.Domain.Racing;
using LTF.Domain.Rnd;
using Xunit;

namespace LTF.Career.Tests;

/// <summary>
/// Dynamic FIA development freezes (Ri4): a freeze proposal, when it passes the regulation ballot at a season
/// boundary, writes its axes/mode into the coming season's <see cref="RegulationSet.DevelopmentFreezes"/>; an
/// unpassed (or repealed) freeze lapses. The favoured axis drives the vote (reusing the M17 ballot heuristic);
/// the frozen axes are a separate payload. Deterministic; a freeze-free ballot leaves the carset untouched.
/// </summary>
public class RegulationFreezeBallotTests
{
    // The vote goes by the favoured axis (aero); the freeze payload targets the engine/gearbox axes.
    private static readonly RegulationProposal Freeze = new()
    {
        Id = "freeze", FavoredAxis = CarAxis.AeroLowSpeed, Magnitude = 0,
        FreezeMode = DevelopmentFreezeMode.InSeasonOnly,
        FreezeAxes = [CarAxis.PowerUnit, CarAxis.GearboxReliability],
    };

    // A two-team carset whose teams both lean the given amount toward aero, so they vote For (positive) or
    // Against (negative) an aero-favoured proposal, carrying it to the ballot.
    private static Carset FreezeBallotCarset(int aeroLean, RegulationProposal proposal)
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 4) with
        {
            PlayerTeamId = "alpha",
            RegulationProposals = [proposal],
        };
        var teams = carset.Teams
            .Select(t => t with
            {
                Research = ResearchState.Empty with { Concept = new ConceptDirection { AeroLean = aeroLean } },
            })
            .ToList();
        return carset with { Teams = teams };
    }

    [Fact]
    public void A_passed_freeze_proposal_writes_the_frozen_axes_for_the_next_season()
    {
        var carset = FreezeBallotCarset(aeroLean: 50, Freeze); // both lean aero → vote For → passes

        var next = RegulationChange.Apply(carset, seed: 7);

        Assert.Equal(2, next.Regulations.DevelopmentFreezes.Count);
        Assert.Contains(next.Regulations.DevelopmentFreezes,
            f => f.Axis == CarAxis.PowerUnit && f.Mode == DevelopmentFreezeMode.InSeasonOnly);
        Assert.Contains(next.Regulations.DevelopmentFreezes, f => f.Axis == CarAxis.GearboxReliability);
    }

    [Fact]
    public void An_unpassed_freeze_proposal_lapses_any_existing_freeze()
    {
        // Teams lean away from aero → vote Against → the freeze proposal fails.
        var carset = FreezeBallotCarset(aeroLean: -50, Freeze) with
        {
            Regulations = new RegulationSet
            {
                DevelopmentFreezes = [new AxisFreeze { Axis = CarAxis.PowerUnit, Mode = DevelopmentFreezeMode.Full }],
            },
        };

        var next = RegulationChange.Apply(carset, seed: 7);

        Assert.Empty(next.Regulations.DevelopmentFreezes); // the unpassed freeze lapses
    }

    [Fact]
    public void A_carset_with_no_proposals_is_inert()
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 4);
        Assert.Same(carset, RegulationChange.Apply(carset, 7));
    }

    [Fact]
    public void The_freeze_ballot_is_deterministic()
    {
        var carset = FreezeBallotCarset(aeroLean: 50, Freeze);
        Assert.Equal(
            RegulationChange.Apply(carset, 7).Regulations.DevelopmentFreezes.Count,
            RegulationChange.Apply(carset, 7).Regulations.DevelopmentFreezes.Count);
    }
}
