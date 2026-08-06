using LTF.Domain;
using LTF.Domain.Management;
using LTF.Domain.Racing;
using LTF.Domain.Rnd;
using LTF.Simulation;

namespace LTF.Career;

/// <summary>One team's vote on a regulation proposal (M17).</summary>
public sealed record TeamVote
{
    public required string TeamId { get; init; }
    public required RegulationVote Vote { get; init; }
}

/// <summary>The result of a regulation ballot (M17): whether the proposal carried and how every team
/// voted. M17 records this; applying the change is M18.</summary>
public sealed record RegulationVoteOutcome
{
    public required string ProposalId { get; init; }
    public required bool Passed { get; init; }
    public required IReadOnlyList<TeamVote> Votes { get; init; }
}

/// <summary>
/// Resolves a regulation proposal put to the teams (M17 / ADR-0010). Each team votes by whether the change
/// favours its car concept — a team leaning toward the proposal's axis votes for it, one leaning away votes
/// against, an undecided team leans on a seeded draw. The player's team can lobby a chosen vote instead.
/// The proposal carries when more teams vote for it than against; the outcome is recorded but not applied —
/// that is M18. Deterministic (seeded, never String.GetHashCode) and fictional (ADR-0003).
/// </summary>
public static class RegulationBallot
{
    /// <summary>Put a proposal to the teams and return how they voted and whether it carried. The player's
    /// team casts <paramref name="playerVote"/> when supplied, otherwise it votes like the rest.</summary>
    public static RegulationVoteOutcome Resolve(
        Carset carset, RegulationProposal proposal, int seed, RegulationVote? playerVote = null)
    {
        var root = new DeterministicRandom(seed);
        var votes = new List<TeamVote>(carset.Teams.Count);
        var forCount = 0;
        var againstCount = 0;
        foreach (var team in carset.Teams)
        {
            var isPlayer = string.CompareOrdinal(team.Id, carset.PlayerTeamId) == 0;
            var vote = isPlayer && playerVote is { } lobby
                ? lobby
                : HeuristicVote(team, proposal, root.Fork(Salt(team.Id)));

            votes.Add(new TeamVote { TeamId = team.Id, Vote = vote });
            if (vote == RegulationVote.For)
            {
                forCount++;
            }
            else if (vote == RegulationVote.Against)
            {
                againstCount++;
            }
        }

        return new RegulationVoteOutcome
        {
            ProposalId = proposal.Id,
            Passed = forCount > againstCount,
            Votes = votes,
        };
    }

    // A team's default stance: for the change if its concept leans toward the favoured axis, against if it
    // leans away, else a seeded lean so an undecided field doesn't all abstain.
    private static RegulationVote HeuristicVote(Team team, RegulationProposal proposal, IRandom rng)
    {
        var lean = LeanFor(team.Research.Concept, proposal.FavoredAxis);
        if (lean > 0)
        {
            return RegulationVote.For;
        }

        if (lean < 0)
        {
            return RegulationVote.Against;
        }

        var draw = rng.NextDouble();
        return draw < 0.4 ? RegulationVote.For : draw < 0.8 ? RegulationVote.Against : RegulationVote.Abstain;
    }

    private static int LeanFor(ConceptDirection concept, CarAxis axis) =>
        CarAxisMap.TargetOf(axis) switch
        {
            CarRatingTarget.Aerodynamics => concept.AeroLean,
            CarRatingTarget.PowerUnit => concept.PowertrainLean,
            _ => 0,
        };

    // A stable, ordinal FNV-1a hash of an id → fork salt. Never String.GetHashCode (process-randomised).
    private static long Salt(string id)
    {
        unchecked
        {
            var hash = 1469598103934665603UL; // FNV-1a offset basis
            foreach (var c in id)
            {
                hash ^= c;
                hash *= 1099511628211UL; // FNV-1a prime
            }

            return (long)hash;
        }
    }
}
