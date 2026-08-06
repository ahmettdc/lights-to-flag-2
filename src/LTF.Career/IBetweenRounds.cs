using LTF.Domain;
using LTF.Domain.Racing;

namespace LTF.Career;

/// <summary>
/// A hook the season engine calls after each round, letting a career evolve the carset mid-season (M15):
/// in-season R&amp;D development, test-day boosts, and anything else that should be felt in later rounds of
/// the same season. Returning the carset unchanged makes the season byte-identical to
/// <see cref="SeasonSimulator.Run"/>. Implementations must be deterministic (seeded from
/// <see cref="BetweenRoundsContext.Seed"/>, never wall-clock or shared RNG).
/// </summary>
public interface IBetweenRounds
{
    /// <summary>Return the carset to contest the remaining rounds with, given the round just completed.</summary>
    Carset AfterRound(Carset current, BetweenRoundsContext context);
}

/// <summary>What the season engine knows about the round it has just finished, for an
/// <see cref="IBetweenRounds"/> progression (M15).</summary>
public readonly record struct BetweenRoundsContext
{
    /// <summary>The round just completed.</summary>
    public required CalendarRound Round { get; init; }

    /// <summary>Zero-based index of the completed round within the calendar.</summary>
    public required int RoundIndex { get; init; }

    /// <summary>Total number of rounds in the season.</summary>
    public required int RoundCount { get; init; }

    /// <summary>The next round's date, or null after the final round — the window test days fall in.</summary>
    public DateOnly? NextRoundDate { get; init; }

    /// <summary>The completed round's deterministic seed, to derive further seeded draws from.</summary>
    public required int Seed { get; init; }
}
