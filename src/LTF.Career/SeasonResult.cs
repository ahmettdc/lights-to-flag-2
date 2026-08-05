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

    public string? DriversChampionId => Standings.DriversChampionId;
    public string? ConstructorsChampionId => Standings.ConstructorsChampionId;
}
