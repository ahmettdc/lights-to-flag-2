using LTF.Simulation.Racing;

namespace LTF.Career;

/// <summary>
/// The outcome of one simulated season (M11): the final championship <see cref="Standings"/> and
/// the per-round race results that produced them, in calendar order. The season's champions are
/// the leaders of the standings.
/// </summary>
public sealed record SeasonResult
{
    public required Standings Standings { get; init; }

    /// <summary>Each round's race result, in calendar order.</summary>
    public required IReadOnlyList<RaceResult> Rounds { get; init; }

    /// <summary>The pole-sitter of each round, in calendar order (null for a round with no pole).
    /// A free byproduct of qualifying, kept so career records can roll up a driver's pole tally
    /// (M11d) — the race results alone don't carry who started first.</summary>
    public IReadOnlyList<string?> PoleSitters { get; init; } = [];

    public string? DriversChampionId => Standings.DriversChampionId;
    public string? ConstructorsChampionId => Standings.ConstructorsChampionId;
}
