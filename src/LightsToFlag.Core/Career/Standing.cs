namespace LightsToFlag.Core.Career;

/// <summary>A row in the drivers' championship table.</summary>
public sealed record DriverStanding
{
    public int Position { get; init; }
    public string CompetitorId { get; init; } = "";
    public string DriverName { get; init; } = "";
    public string TeamName { get; init; } = "";
    public double Points { get; init; }
    public int Wins { get; init; }
}

/// <summary>A row in the constructors' championship table.</summary>
public sealed record ConstructorStanding
{
    public int Position { get; init; }
    public int TeamNumber { get; init; }
    public string TeamName { get; init; } = "";
    public double Points { get; init; }
    public int Wins { get; init; }
}
