namespace LightsToFlag.Core.Domain;

/// <summary>
/// Immutable circuit "card" as defined by a carset (see <c>Circuitdata.txt</c>,
/// schema in <c>Carsetmaker/circuitdatadesc.txt</c>).
/// The per-circuit record has a variable width: a fixed prefix, then one base
/// laptime per class, then 6 corner strings and 3 overtaking strings.
/// </summary>
public sealed record CircuitSpec
{
    public required string Name { get; init; }
    public string Sponsor { get; init; } = "";
    public string Venue { get; init; } = "";
    public TrackKind Kind { get; init; } = TrackKind.Track;

    /// <summary>True when this event immediately follows the previous round (back-to-back).</summary>
    public bool BackToBack { get; init; }

    /// <summary>Number of grid places reversed from pole (0 = none).</summary>
    public int ReversedGridPlaces { get; init; }

    /// <summary>Which points format to use (1 = primary, 2 = secondary).</summary>
    public int PointsFormat { get; init; } = 1;

    public double LapRecord { get; init; }
    public string LapRecordHolder { get; init; } = "";

    /// <summary>Race distance in laps (0 for a timed race).</summary>
    public int Laps { get; init; }

    /// <summary>Time limit in minutes (for timed / time-limited races).</summary>
    public int Minutes { get; init; }

    /// <summary>Free-text lap length (e.g. "5.303km"), as authored.</summary>
    public string LapLength { get; init; } = "";

    /// <summary>Free-text race length (e.g. "307.574km"), as authored.</summary>
    public string RaceLength { get; init; } = "";

    public int RacesHeld { get; init; }
    public string Debut { get; init; } = "";

    // --- Simulation characteristics (as authored) ---
    public int TyreWear { get; init; }
    public int Overtaking { get; init; }
    public int SafetyCarLikelihood { get; init; }
    public int WeatherChangeability { get; init; }
    public int TopSpeeds { get; init; }
    public int HighSpeedCorners { get; init; }
    public int LowSpeedCorners { get; init; }
    public int Difficulty { get; init; }
    public double FuelPenaltyPerLap { get; init; }
    public double RubberInPerLap { get; init; }
    public int AttritionRate { get; init; }
    public int PitLaneSeconds { get; init; }

    /// <summary>1 = short run to first corner, 2 = long run.</summary>
    public int RunToFirstCorner { get; init; }

    public int MandatoryPitStops { get; init; }
    public double BallastPenaltyPerKgPerLap { get; init; }

    /// <summary>Fuel tank size as a percentage of race distance.</summary>
    public int FuelTankPercent { get; init; }

    public bool HalfTyreChangesAllowed { get; init; }
    public int DaybreakMinute { get; init; }
    public int NightfallMinute { get; init; }

    /// <summary>Base laptime in seconds, one entry per class.</summary>
    public IReadOnlyList<double> BaseLaptimeByClass { get; init; } = Array.Empty<double>();

    /// <summary>Six short strings describing parts of the circuit.</summary>
    public IReadOnlyList<string> CornerStrings { get; init; } = Array.Empty<string>();

    /// <summary>Three longer strings describing overtaking opportunities.</summary>
    public IReadOnlyList<string> OvertakingStrings { get; init; } = Array.Empty<string>();
}
