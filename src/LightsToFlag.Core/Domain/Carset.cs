namespace LightsToFlag.Core.Domain;

/// <summary>
/// Aggregate root bundling every immutable "card" that makes up a carset, plus
/// the folder the assets (car / driver / circuit images) live in. This is the
/// canonical, engine-facing representation a <c>Data.ICarsetLoader</c> produces.
/// </summary>
public sealed record Carset
{
    /// <summary>Display name (the carset folder name, e.g. "F1 2019").</summary>
    public required string Name { get; init; }

    /// <summary>Absolute path to the carset folder (asset base).</summary>
    public required string SourcePath { get; init; }

    public required RulesSet Rules { get; init; }
    public required Coefficients Coefficients { get; init; }

    /// <summary>Full race drivers (the first <c>DriverCount</c> records of <c>Driverdata.txt</c>).</summary>
    public required IReadOnlyList<DriverRating> Drivers { get; init; }

    /// <summary>Reserve / rookie pool (the trailing <c>SpareCount</c> abbreviated records).</summary>
    public required IReadOnlyList<RookieRating> Reserves { get; init; }

    public required IReadOnlyList<TeamRating> Teams { get; init; }
    public required IReadOnlyList<CircuitSpec> Circuits { get; init; }
}
