namespace LTF.Domain.Racing;

/// <summary>One driver's final line in a completed season (M24): where they placed in that year's drivers'
/// championship and the tallies behind it. A compact, value-typed snapshot so a season archive round-trips a
/// save byte-stably.</summary>
public sealed record DriverSeasonLine
{
    /// <summary>The driver (a competitor/driver id).</summary>
    public required string DriverId { get; init; }

    /// <summary>Final championship position (1 = champion).</summary>
    public required int Position { get; init; }

    /// <summary>Championship points scored across the season.</summary>
    public required int Points { get; init; }

    public int Wins { get; init; }
    public int Podiums { get; init; }
}

/// <summary>One team's final line in a completed season (M24): its place in that year's constructors'
/// championship and the points and wins behind it.</summary>
public sealed record ConstructorSeasonLine
{
    /// <summary>The team.</summary>
    public required string TeamId { get; init; }

    /// <summary>Final championship position (1 = champion).</summary>
    public required int Position { get; init; }

    /// <summary>Championship points scored across the season.</summary>
    public required int Points { get; init; }

    public int Wins { get; init; }
}

/// <summary>
/// The archived record of one completed season (M24): the year, its two champions, and the final championship
/// tables that produced them. Built at the season boundary (<c>SeasonArchive.Append</c>) from the season's result
/// and appended to <see cref="Carset.SeasonHistory"/>, which accumulates across a career and is never
/// reconstructed — the raw per-round results are discarded at rollover, so this is the only lasting trace of a
/// season. Value-typed throughout, so it round-trips a save byte-stably.
/// </summary>
public sealed record SeasonRecord
{
    /// <summary>The calendar year the season was raced in (its first round's year).</summary>
    public required int Year { get; init; }

    /// <summary>The drivers' champion (a competitor/driver id); empty for a season with no classified drivers.</summary>
    public string DriversChampionId { get; init; } = "";

    /// <summary>The constructors' champion (a team id); empty for a season with no classified teams.</summary>
    public string ConstructorsChampionId { get; init; } = "";

    /// <summary>The final drivers' championship, best-first.</summary>
    public IReadOnlyList<DriverSeasonLine> Drivers { get; init; } = [];

    /// <summary>The final constructors' championship, best-first.</summary>
    public IReadOnlyList<ConstructorSeasonLine> Constructors { get; init; } = [];
}
