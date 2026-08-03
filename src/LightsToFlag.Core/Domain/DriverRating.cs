namespace LightsToFlag.Core.Domain;

/// <summary>
/// Immutable driver "card" as defined by a carset (see <c>Driverdata.txt</c>,
/// schema in <c>Carsetmaker/driverdatadesc.txt</c> — 24 fields per driver).
/// Skill ratings are on the carset's native 1–10 scale as authored.
/// </summary>
public sealed record DriverRating
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string FullName => $"{FirstName} {LastName}".Trim();

    public int Age { get; init; }

    /// <summary>Racing number shown on the car.</summary>
    public int Number { get; init; }

    /// <summary>1-based index into the team list this driver races for.</summary>
    public int TeamNumber { get; init; }

    /// <summary>Seat within the team (1 or 2).</summary>
    public int NumberWithinTeam { get; init; }

    // --- Skills (1–10 as authored) ---
    public int Pace { get; init; }
    public int Consistency { get; init; }
    public int Concentration { get; init; }
    public int WetWeather { get; init; }
    public int Overtaking { get; init; }
    public int Smoothness { get; init; }
    public int Feedback { get; init; }
    public int Teamwork { get; init; }
    public int Qualifying { get; init; }

    public string Nationality { get; init; } = "";

    /// <summary>Free-text debut race description (e.g. "Australia, 2007").</summary>
    public string Debut { get; init; } = "";

    // --- Career history ---
    public int Races { get; init; }
    public int Wins { get; init; }
    public int Podiums { get; init; }
    public int Poles { get; init; }
    public int FastestLaps { get; init; }
    public int Championships { get; init; }
    public double CareerPoints { get; init; }
}
