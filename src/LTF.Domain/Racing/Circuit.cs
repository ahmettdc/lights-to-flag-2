using LTF.Domain.Common;

namespace LTF.Domain.Racing;

/// <summary>
/// Immutable circuit "card". Ratings describe how the track stresses car and driver;
/// the simulation (Phase 1) turns them into lap time and event probabilities.
/// </summary>
public sealed record Circuit
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string Country { get; init; } = "";
    public TrackKind Kind { get; init; } = TrackKind.RoadCourse;

    /// <summary>Race distance in laps.</summary>
    public required int Laps { get; init; }

    /// <summary>Length of one lap in kilometres.</summary>
    public required double LapDistanceKm { get; init; }

    /// <summary>A representative clean-air lap time in seconds (reference pace).</summary>
    public required double BaseLapTimeSeconds { get; init; }

    // --- Character (all on the 1–100 scale) ---

    /// <summary>How hard the track is on tyres.</summary>
    public Rating TyreStress { get; init; } = new(50);

    /// <summary>How easy overtaking is.</summary>
    public Rating Overtaking { get; init; } = new(50);

    /// <summary>How much engine power matters here.</summary>
    public Rating PowerSensitivity { get; init; } = new(50);

    /// <summary>How much aerodynamic downforce matters here.</summary>
    public Rating DownforceSensitivity { get; init; } = new(50);

    /// <summary>Likelihood of a safety car / caution.</summary>
    public Rating SafetyCarLikelihood { get; init; } = new(50);

    /// <summary>How changeable the weather tends to be.</summary>
    public Rating WeatherVariability { get; init; } = new(50);

    /// <summary>Total distance of the race in kilometres.</summary>
    public double RaceDistanceKm => Laps * LapDistanceKm;
}
