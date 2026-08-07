using LTF.Domain.Common;

namespace LTF.Domain.Racing;

/// <summary>
/// A player's pre-race decision for one of their drivers at one round (M23b): the tyre compound to start on.
/// Keyed by round number and driver id so it feeds the race simulator for the matching round only — each
/// choice affects its own race and nothing else. Lives on <see cref="Carset.PlayerRaceStrategies"/>, riding
/// the season-start carset through to the live carset (R&amp;D never touches it), so a choice persists through
/// save/load and reconstruction. Value-typed (an int, a string and an enum), so it round-trips a save exactly.
/// </summary>
public sealed record RaceStrategy
{
    /// <summary>The calendar round number this choice applies to.</summary>
    public required int Round { get; init; }

    /// <summary>The driver the choice is for (a competitor id — equal to the driver id).</summary>
    public required string DriverId { get; init; }

    /// <summary>The compound the driver starts the race on.</summary>
    public TyreCompound Compound { get; init; } = TyreCompound.Medium;
}
