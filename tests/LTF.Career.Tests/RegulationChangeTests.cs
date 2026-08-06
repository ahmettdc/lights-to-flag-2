using System.Linq;
using LTF.Domain;
using LTF.Domain.Management;
using LTF.Domain.Rnd;
using Xunit;

namespace LTF.Career.Tests;

public class RegulationChangeTests
{
    // A floor proposal that passes (both teams lean aero, so both vote for it); alpha is well prepared,
    // bravo is not; the unreadiness penalty is live.
    private static Carset RegulationCarset(double penalty = 0.5)
    {
        var carset = CareerFixtures.Carset();
        var rules = carset.Rules with { RegulationUnreadinessPenalty = penalty };
        var alpha = carset.Teams[0] with { Research = LeaningAero(readiness: 90) };
        var bravo = carset.Teams[1] with { Research = LeaningAero(readiness: 10) };
        var proposal = new RegulationProposal
        {
            Id = "floor", Description = "Raise the floor", FavoredAxis = CarAxis.AeroFloor, Magnitude = 50,
        };
        return carset with { Rules = rules, Teams = [alpha, bravo], RegulationProposals = [proposal] };
    }

    private static ResearchState LeaningAero(int readiness) => ResearchState.Empty with
    {
        Concept = new ConceptDirection { AeroLean = 50 }, // → votes for an aero proposal
        RegulationReadiness = readiness,
    };

    private static Team Find(Carset carset, string id) => carset.Teams.Single(t => t.Id == id);

    [Fact]
    public void A_passed_change_sets_an_unprepared_team_back_further_than_a_prepared_one()
    {
        var before = RegulationCarset();
        var aeroBefore = Find(before, "alpha").Car.Aerodynamics.Value; // both start at 70

        var after = RegulationChange.Apply(before, seed: 7);

        var alphaDrop = aeroBefore - Find(after, "alpha").Car.Aerodynamics.Value;
        var bravoDrop = aeroBefore - Find(after, "bravo").Car.Aerodynamics.Value;
        Assert.True(alphaDrop > 0);            // even a prepared team feels the change a little
        Assert.True(bravoDrop > alphaDrop);    // the unprepared team is set back far more
    }

    [Fact]
    public void With_the_penalty_coefficient_off_the_carset_is_untouched()
    {
        var carset = RegulationCarset(penalty: 0.0); // proposals present, but the penalty is disabled

        Assert.Same(carset, RegulationChange.Apply(carset, 7));
    }

    [Fact]
    public void With_no_proposals_the_carset_is_untouched()
    {
        var carset = CareerFixtures.Carset(); // ships no regulation proposals

        Assert.Same(carset, RegulationChange.Apply(carset, 7));
    }

    [Fact]
    public void Regulation_application_is_deterministic()
    {
        var carset = RegulationCarset();

        Assert.Equal(Key(RegulationChange.Apply(carset, 7)), Key(RegulationChange.Apply(carset, 7)));
    }

    private static string Key(Carset carset) => string.Join("|", carset.Teams
        .Select(t => $"{t.Id}:{t.Car.Aerodynamics.Value}"));
}
