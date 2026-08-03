using LTF.Domain.Common;

namespace LTF.Domain.Racing;

/// <summary>
/// A driver's on-track skill set, on the shared 1–100 <see cref="Rating"/> scale.
/// Names match the UI mockup so the interface binds to them directly later.
/// </summary>
public sealed record DriverAttributes
{
    public required Rating Pace { get; init; }
    public required Rating Racecraft { get; init; }
    public required Rating Consistency { get; init; }
    public required Rating TyreManagement { get; init; }
    public required Rating WetWeather { get; init; }
    public required Rating Feedback { get; init; }

    /// <summary>
    /// Single headline rating (the "RATING" shown in the mockup), a weighted blend
    /// that leans on outright pace. Rounded to the nearest whole point.
    /// </summary>
    public int Overall => (int)Math.Round(
        (Pace.Value * 0.34)
        + (Racecraft.Value * 0.20)
        + (Consistency.Value * 0.16)
        + (TyreManagement.Value * 0.14)
        + (WetWeather.Value * 0.08)
        + (Feedback.Value * 0.08));
}

/// <summary>A driver's accumulated career record.</summary>
public sealed record DriverCareer
{
    public int Races { get; init; }
    public int Wins { get; init; }
    public int Podiums { get; init; }
    public int Poles { get; init; }
    public int FastestLaps { get; init; }
    public int Championships { get; init; }
    public double Points { get; init; }

    /// <summary>An empty record for a driver making their debut.</summary>
    public static DriverCareer None { get; } = new();
}

/// <summary>
/// Immutable driver "card". Static talent lives in <see cref="Attributes"/>; the
/// values that move during a career (morale, reputation, record) sit alongside it.
/// </summary>
public sealed record Driver
{
    /// <summary>Stable identifier, unique within a carset (e.g. "ferreira_mateo").</summary>
    public required string Id { get; init; }

    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string FullName => $"{FirstName} {LastName}".Trim();

    public required int Age { get; init; }
    public string Nationality { get; init; } = "";

    /// <summary>Racing number carried on the car.</summary>
    public int Number { get; init; }

    public required DriverAttributes Attributes { get; init; }

    /// <summary>Current morale (dynamic; the mockup's "MORALE").</summary>
    public Rating Morale { get; init; } = new(50);

    /// <summary>Standing in the paddock, gating which seats open up.</summary>
    public Rating Reputation { get; init; } = new(50);

    public DriverCareer Career { get; init; } = DriverCareer.None;
}
