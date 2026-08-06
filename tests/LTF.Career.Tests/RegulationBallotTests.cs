using System.Linq;
using LTF.Domain;
using LTF.Domain.Management;
using LTF.Domain.Rnd;
using Xunit;

namespace LTF.Career.Tests;

public class RegulationBallotTests
{
    private static readonly RegulationProposal AeroProposal = new()
    {
        Id = "aero-rule", FavoredAxis = CarAxis.AeroLowSpeed, Magnitude = 50,
    };

    // A two-team carset (alpha the player) with each team's car concept leaning a chosen amount toward aero.
    private static Carset BallotCarset(int alphaAeroLean, int bravoAeroLean)
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 4) with { PlayerTeamId = "alpha" };
        var teams = carset.Teams
            .Select(t => t with
            {
                Research = ResearchState.Empty with
                {
                    Concept = new ConceptDirection { AeroLean = t.Id == "alpha" ? alphaAeroLean : bravoAeroLean },
                },
            })
            .ToList();
        return carset with { Teams = teams };
    }

    [Fact]
    public void Teams_leaning_toward_the_favored_axis_carry_the_vote()
    {
        var carset = BallotCarset(alphaAeroLean: 50, bravoAeroLean: 50); // both lean aero

        var outcome = RegulationBallot.Resolve(carset, AeroProposal, seed: 7);

        Assert.True(outcome.Passed);
        Assert.All(outcome.Votes, v => Assert.Equal(RegulationVote.For, v.Vote));
    }

    [Fact]
    public void A_team_leaning_away_votes_against()
    {
        var carset = BallotCarset(alphaAeroLean: 50, bravoAeroLean: -50);

        var outcome = RegulationBallot.Resolve(carset, AeroProposal, seed: 7);

        Assert.Equal(RegulationVote.For, outcome.Votes.Single(v => v.TeamId == "alpha").Vote);
        Assert.Equal(RegulationVote.Against, outcome.Votes.Single(v => v.TeamId == "bravo").Vote);
        Assert.False(outcome.Passed); // 1 for, 1 against → not carried
    }

    [Fact]
    public void A_player_lobby_overrides_the_players_own_vote()
    {
        var carset = BallotCarset(alphaAeroLean: 50, bravoAeroLean: 50); // both would vote for

        // The player (alpha) lobbies against, flipping the tally to 1 for, 1 against.
        var outcome = RegulationBallot.Resolve(carset, AeroProposal, seed: 7, RegulationVote.Against);

        Assert.Equal(RegulationVote.Against, outcome.Votes.Single(v => v.TeamId == "alpha").Vote);
        Assert.Equal(RegulationVote.For, outcome.Votes.Single(v => v.TeamId == "bravo").Vote);
        Assert.False(outcome.Passed);
    }

    [Fact]
    public void Resolving_a_ballot_is_deterministic()
    {
        // Neutral concepts → the vote is seeded, so determinism actually exercises the RNG.
        var carset = BallotCarset(alphaAeroLean: 0, bravoAeroLean: 0);

        Assert.Equal(
            Key(RegulationBallot.Resolve(carset, AeroProposal, 7)),
            Key(RegulationBallot.Resolve(carset, AeroProposal, 7)));
    }

    private static string Key(RegulationVoteOutcome outcome) =>
        $"{outcome.ProposalId}:{outcome.Passed}:" +
        string.Join(",", outcome.Votes.Select(v => $"{v.TeamId}={v.Vote}"));
}
