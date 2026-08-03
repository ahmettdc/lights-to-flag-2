namespace LightsToFlag.Core.Domain;

/// <summary>
/// Immutable team / car "card" as defined by a carset (see <c>Teamdata.txt</c>,
/// schema in <c>Carsetmaker/teamdatadesc.txt</c> — 26 fields per team).
/// Performance ratings are on the carset's native 1–10 scale as authored.
/// </summary>
public sealed record TeamRating
{
    public required string Name { get; init; }
    public string TitleSponsor { get; init; } = "";
    public string EngineBrand { get; init; } = "";
    public string ChassisName { get; init; } = "";
    public string EngineName { get; init; } = "";

    /// <summary>1-based class this team competes in (1 for single-class series).</summary>
    public int Class { get; init; } = 1;

    public string TyreManufacturer { get; init; } = "";

    // --- Car performance (1–10 as authored) ---
    public int Aerodynamics { get; init; }
    public int MechanicalGrip { get; init; }
    public int Engine { get; init; }
    public int EaseOnTyres { get; init; }
    public int Reliability { get; init; }
    public int WetWeather { get; init; }
    public int Setup { get; init; }
    public int Qualifying { get; init; }
    public int Resources { get; init; }

    public string Base { get; init; } = "";
    public string Principal { get; init; } = "";
    public string Debut { get; init; } = "";

    // --- History ---
    public int Races { get; init; }
    public int Wins { get; init; }
    public int Poles { get; init; }
    public int FastestLaps { get; init; }
    public double Points { get; init; }
    public int DriversChampionships { get; init; }
    public int ConstructorsChampionships { get; init; }
}
