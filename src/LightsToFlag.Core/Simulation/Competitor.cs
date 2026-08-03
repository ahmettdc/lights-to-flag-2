using LightsToFlag.Core.Domain;

namespace LightsToFlag.Core.Simulation;

/// <summary>
/// A single car+driver entry as the simulator sees it: the immutable ratings
/// plus the per-event modifiers (ballast, setup quality) that career mode feeds in.
/// </summary>
public sealed record Competitor
{
    /// <summary>Stable identity across a weekend (e.g. the car number as a string).</summary>
    public required string Id { get; init; }

    public required DriverRating Driver { get; init; }
    public required TeamRating Car { get; init; }

    /// <summary>Success ballast carried this event, in kilograms.</summary>
    public int BallastKg { get; init; }

    /// <summary>Setup quality earned in practice, 0 (poor) … 1 (dialled in).</summary>
    public double SetupQuality { get; init; }

    /// <summary>0-based class index into <see cref="CircuitSpec.BaseLaptimeByClass"/>.</summary>
    public int ClassIndex { get; init; }
}
