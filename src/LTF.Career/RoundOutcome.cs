using LTF.Simulation.Qualifying;
using LTF.Simulation.Racing;

namespace LTF.Career;

/// <summary>
/// The outcome of a single season round (M21): the race <see cref="Result"/>, who took pole, and the full
/// qualifying <see cref="Qualifying"/> grid (M23d). Returned by <see cref="SeasonSimulator.RunRound"/> so the
/// live career can run one round at a time and fold the result into the standings, exactly as the whole-season
/// loop does. The qualifying result is carried so the race-weekend screen can show a qualifying tab without
/// re-running the session; it is reconstructed (never persisted), like the standings and the race telemetry.
/// </summary>
public sealed record RoundOutcome(RaceResult Result, string? PoleSitterId, QualifyingResult Qualifying);
