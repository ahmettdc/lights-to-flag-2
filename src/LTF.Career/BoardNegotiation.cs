using System.Linq;
using LTF.Domain.Common;
using LTF.Domain.Management;
using LTF.Simulation;

namespace LTF.Career;

/// <summary>How the board answered a counter-offer.</summary>
public enum BoardVerdict
{
    /// <summary>The board took the player's proposal as-is.</summary>
    Accepted,

    /// <summary>The board met the player partway with a firmer target.</summary>
    Countered,

    /// <summary>The board held its opening objective.</summary>
    Rejected,
}

/// <summary>The board's answer to a proposal: the outcome and how many notches of easing it agreed to
/// (0 = the anchor; positive = eased toward the player).</summary>
public sealed record BoardNegotiationResult(BoardVerdict Outcome, int AgreedNotches);

/// <summary>
/// Pre-career board-objective negotiation (M20 / ADR-0025). The board opens with an objective drawn from
/// its ownership (or its authored one); the player counters by asking for it to be <em>eased</em> by some
/// number of notches, and the board accepts, counters partway, or holds firm. Deterministic — a seeded
/// jitter only (like <see cref="BoardReview"/>), no wall clock — and works in unit-agnostic notches, so the
/// per-objective target conversion stays in the UI and one tolerance model covers every objective kind.
/// This is career-setup arithmetic; it never touches the race engine, so the golden digest is unaffected.
/// </summary>
public static class BoardNegotiation
{
    /// <summary>The board's opening objective: its first authored one, else its ownership default.</summary>
    public static Objective Opening(TeamBoard board) =>
        board.Objectives.Count > 0 ? board.Objectives[0] : BoardReview.DefaultObjective(board.Ownership);

    /// <summary>
    /// Answer a proposal that eases the board's anchor by <paramref name="notches"/> (negative = a tougher
    /// target the board is always glad to take). Returns the outcome and the notches the board agrees to.
    /// </summary>
    public static BoardNegotiationResult Evaluate(TeamBoard board, int notches, int seed)
    {
        if (notches <= 0)
        {
            return new BoardNegotiationResult(BoardVerdict.Accepted, notches);
        }

        var tolerance = Tolerance(board, seed);
        if (notches <= tolerance)
        {
            return new BoardNegotiationResult(BoardVerdict.Accepted, notches);
        }

        if (notches <= tolerance * 2)
        {
            return new BoardNegotiationResult(BoardVerdict.Countered, tolerance);
        }

        return new BoardNegotiationResult(BoardVerdict.Rejected, 0);
    }

    // How many notches of easing the board will grant: an ownership base, loosened by risk-averse members
    // and tightened by ambitious/impatient ones, plus a small seeded jitter so a marginal ask is not a cliff.
    private static int Tolerance(TeamBoard board, int seed)
    {
        var baseTolerance = board.Ownership switch
        {
            OwnershipType.DevelopmentProject => 3,
            OwnershipType.SponsorHeavy => 2,
            OwnershipType.LegacyFamily => 2,
            _ => 1, // RacingOwner / FinanceBoard / ManufacturerBacked are demanding
        };

        var ambitious = board.Members.Count(m => m.Traits.HasFlag(BoardMemberTraits.Ambitious));
        var impatient = board.Members.Count(m => m.Traits.HasFlag(BoardMemberTraits.Impatient));
        var riskAverse = board.Members.Count(m => m.Traits.HasFlag(BoardMemberTraits.RiskAverse));

        var jitter = (int)System.Math.Round(new DeterministicRandom(seed).NextDouble()); // 0 or 1
        return System.Math.Max(0, baseTolerance + riskAverse - ambitious - impatient + jitter);
    }
}
