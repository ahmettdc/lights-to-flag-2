namespace LTF.Career;

/// <summary>One driver's line in the drivers' championship (M11).</summary>
public sealed record DriverStanding
{
    public required int Position { get; init; }
    public required string DriverId { get; init; }
    public required int Points { get; init; }
    public required int Wins { get; init; }
    public required int Podiums { get; init; }
}

/// <summary>One team's line in the constructors' championship (M11).</summary>
public sealed record ConstructorStanding
{
    public required int Position { get; init; }
    public required string TeamId { get; init; }
    public required int Points { get; init; }
    public required int Wins { get; init; }
}

/// <summary>
/// A championship table at a point in a season (M11): the drivers' and the constructors'
/// standings, each ordered best-first, so the leader of each is the (provisional) champion.
/// </summary>
public sealed record Standings
{
    public required IReadOnlyList<DriverStanding> Drivers { get; init; }
    public required IReadOnlyList<ConstructorStanding> Constructors { get; init; }

    /// <summary>The driver leading the championship, or null if there are none.</summary>
    public string? DriversChampionId => Drivers.Count > 0 ? Drivers[0].DriverId : null;

    /// <summary>The team leading the championship, or null if there are none.</summary>
    public string? ConstructorsChampionId => Constructors.Count > 0 ? Constructors[0].TeamId : null;
}
