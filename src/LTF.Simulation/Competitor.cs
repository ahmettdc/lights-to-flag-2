using LTF.Domain.Racing;

namespace LTF.Simulation;

/// <summary>
/// One entry in a session: a driver paired with the car they are running. Flattened from
/// the carset (a team's car + each of its drivers) so the simulation works with a single
/// list rather than walking team → driver references.
/// </summary>
public sealed record Competitor
{
    /// <summary>The driver's id (also the competitor's id within a session).</summary>
    public required string Id { get; init; }

    public required string TeamId { get; init; }

    /// <summary>The class this competitor races in (M9); empty = single class.</summary>
    public string Class { get; init; } = "";

    public required Driver Driver { get; init; }
    public required Car Car { get; init; }
}
