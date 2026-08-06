using System.Linq;
using LTF.Career;
using LTF.Domain.Common;
using LTF.Domain.Management;
using Xunit;

namespace LTF.Career.Tests;

/// <summary>
/// Pre-career board-objective negotiation (M20c): the board accepts a fair counter, meets a firmer one
/// partway, and holds against a greedy one — deterministically, and coloured by ownership and member
/// traits. Career-setup arithmetic; never on the golden-race path.
/// </summary>
public class BoardNegotiationTests
{
    private static TeamBoard Board(OwnershipType ownership, params BoardMemberTraits[] traits) => new()
    {
        TeamId = "t",
        Ownership = ownership,
        Members = traits.Select((tr, i) => Member($"m{i}", tr)).ToList(),
    };

    private static BoardMember Member(string id, BoardMemberTraits traits) => new()
    {
        Id = id,
        SportingPriority = new(50),
        FinancialPriority = new(50),
        LongTermPriority = new(50),
        BrandPriority = new(50),
        DriverDevPriority = new(50),
        RiskTolerance = new(50),
        Traits = traits,
    };

    [Fact]
    public void Opening_uses_the_authored_objective_when_present()
    {
        var board = Board(OwnershipType.RacingOwner) with
        {
            Objectives = [new Objective { Kind = ObjectiveKind.RaceWins, Target = 5 }],
        };

        var opening = BoardNegotiation.Opening(board);

        Assert.Equal(ObjectiveKind.RaceWins, opening.Kind);
        Assert.Equal(5, opening.Target);
    }

    [Fact]
    public void Opening_falls_back_to_the_ownership_default() =>
        Assert.Equal(
            BoardReview.DefaultObjective(OwnershipType.ManufacturerBacked),
            BoardNegotiation.Opening(Board(OwnershipType.ManufacturerBacked)));

    [Fact]
    public void A_tougher_or_equal_proposal_is_always_accepted()
    {
        var board = Board(OwnershipType.RacingOwner);

        Assert.Equal(BoardVerdict.Accepted, BoardNegotiation.Evaluate(board, 0, 1).Outcome);

        var tougher = BoardNegotiation.Evaluate(board, -2, 1);
        Assert.Equal(BoardVerdict.Accepted, tougher.Outcome);
        Assert.Equal(-2, tougher.AgreedNotches);
    }

    [Fact]
    public void A_modest_ask_within_tolerance_is_accepted()
    {
        // DevelopmentProject base tolerance 3 (+0/1 jitter): a 2-notch ask is always within reach.
        var result = BoardNegotiation.Evaluate(Board(OwnershipType.DevelopmentProject), 2, 7);

        Assert.Equal(BoardVerdict.Accepted, result.Outcome);
        Assert.Equal(2, result.AgreedNotches);
    }

    [Fact]
    public void A_mid_ask_is_countered_partway()
    {
        // Tolerance 3 or 4: a 5-notch ask lands in (tolerance, 2*tolerance] either way.
        var result = BoardNegotiation.Evaluate(Board(OwnershipType.DevelopmentProject), 5, 7);

        Assert.Equal(BoardVerdict.Countered, result.Outcome);
        Assert.InRange(result.AgreedNotches, 1, 4);
    }

    [Fact]
    public void A_greedy_ask_is_rejected_back_to_the_anchor()
    {
        var result = BoardNegotiation.Evaluate(Board(OwnershipType.DevelopmentProject), 12, 7);

        Assert.Equal(BoardVerdict.Rejected, result.Outcome);
        Assert.Equal(0, result.AgreedNotches);
    }

    [Fact]
    public void Evaluation_is_deterministic()
    {
        var board = Board(OwnershipType.SponsorHeavy);

        Assert.Equal(BoardNegotiation.Evaluate(board, 3, 99), BoardNegotiation.Evaluate(board, 3, 99));
    }

    [Fact]
    public void An_ambitious_board_is_tougher_than_a_risk_averse_one()
    {
        var lenient = Board(OwnershipType.DevelopmentProject,
            BoardMemberTraits.RiskAverse, BoardMemberTraits.RiskAverse, BoardMemberTraits.RiskAverse);
        var demanding = Board(OwnershipType.RacingOwner,
            BoardMemberTraits.Ambitious, BoardMemberTraits.Ambitious, BoardMemberTraits.Ambitious);

        // A 6-notch ask: the lenient board (base 3 + 3 risk-averse) grants it; the demanding board holds.
        Assert.Equal(BoardVerdict.Accepted, BoardNegotiation.Evaluate(lenient, 6, 5).Outcome);
        Assert.Equal(BoardVerdict.Rejected, BoardNegotiation.Evaluate(demanding, 6, 5).Outcome);
    }
}
