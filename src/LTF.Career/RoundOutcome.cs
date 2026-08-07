using LTF.Simulation.Racing;

namespace LTF.Career;

/// <summary>
/// The outcome of a single season round (M21): the race <see cref="Result"/> and who took pole. Returned by
/// <see cref="SeasonSimulator.RunRound"/> so the live career can run one round at a time and fold the result
/// into the standings, exactly as the whole-season loop does.
/// </summary>
public sealed record RoundOutcome(RaceResult Result, string? PoleSitterId);
