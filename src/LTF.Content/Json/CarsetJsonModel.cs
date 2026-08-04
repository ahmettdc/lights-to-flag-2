using System.Text.Json;

namespace LTF.Content.Json;

// Data-transfer shapes for a carset.json file. Everything is nullable so the mapper
// (CarsetLoader) can report a precise, friendly error for anything missing rather than
// letting the deserializer throw. Ratings are plain ints here; the mapper turns them into
// validated Rating value objects.

internal sealed class CarsetJson
{
    public int? SchemaVersion { get; set; }
    public string? Id { get; set; }
    public string? Name { get; set; }
    public RulesJson? Rules { get; set; }
    public BalanceJson? Balance { get; set; }
    public List<TyreJson>? Tyres { get; set; }
    public List<CircuitJson>? Circuits { get; set; }
    public List<CalendarRoundJson>? Calendar { get; set; }
    public List<TeamJson>? Teams { get; set; }
    public List<DriverJson>? Drivers { get; set; }
    public List<DriverJson>? Reserves { get; set; }

    /// <summary>Forward hook (ADR-0010): parsed and retained but not yet interpreted.</summary>
    public JsonElement? Regulations { get; set; }
}

internal sealed class RulesJson
{
    public string? SeriesName { get; set; }
    public string? Qualifying { get; set; }
    public int? RetirementAge { get; set; }
    public bool? RefuellingAllowed { get; set; }
    public int? MandatoryPitStops { get; set; }
    public bool? BothDryCompoundsRequired { get; set; }
    public int? GridPenaltyPerExtraComponent { get; set; }
    public Dictionary<string, int>? ComponentAllocation { get; set; }
    public PointsJson? Points { get; set; }
}

internal sealed class PointsJson
{
    public List<int>? RacePoints { get; set; }
    public List<int>? SprintPoints { get; set; }
    public int? PolePoint { get; set; }
    public int? FastestLapPoint { get; set; }
}

internal sealed class BalanceJson
{
    public double? DriverPaceWeight { get; set; }
    public double? CarPaceWeight { get; set; }
    public double? RandomnessSpreadSeconds { get; set; }
    public double? TyreWearPerLap { get; set; }
    public double? ReliabilityFailureRate { get; set; }
    public double? SafetyCarBaseChance { get; set; }
    public double? OvertakeBaseChance { get; set; }
    public double? WetPaceLoss { get; set; }
    public double? FuelLoadPenaltySeconds { get; set; }
}

internal sealed class TyreJson
{
    public string? Compound { get; set; }
    public string? Code { get; set; }
    public int? Grip { get; set; }
    public int? Durability { get; set; }
}

internal sealed class CircuitJson
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Country { get; set; }
    public string? Kind { get; set; }
    public int? Laps { get; set; }
    public double? LapDistanceKm { get; set; }
    public double? BaseLapTimeSeconds { get; set; }
    public int? TyreStress { get; set; }
    public int? Overtaking { get; set; }
    public int? PowerSensitivity { get; set; }
    public int? DownforceSensitivity { get; set; }
    public int? SafetyCarLikelihood { get; set; }
    public int? WeatherVariability { get; set; }
}

internal sealed class CalendarRoundJson
{
    public int? Round { get; set; }
    public string? CircuitId { get; set; }
    public string? Date { get; set; }
    public bool? Sprint { get; set; }
}

internal sealed class TeamJson
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? ShortName { get; set; }
    public string? Base { get; set; }
    public string? Principal { get; set; }
    public string? Nationality { get; set; }
    public CarJson? Car { get; set; }
    public List<string>? DriverIds { get; set; }
    public FacilitiesJson? Facilities { get; set; }
    public FinancesJson? Finances { get; set; }
    public int? ChampionshipsWon { get; set; }
    public int? RaceWins { get; set; }
}

internal sealed class CarJson
{
    public int? Aerodynamics { get; set; }
    public int? Chassis { get; set; }
    public int? PowerUnit { get; set; }
    public int? TyreGentleness { get; set; }
    public int? Reliability { get; set; }
    public string? EngineSupplier { get; set; }
}

internal sealed class FacilitiesJson
{
    public int? WindTunnel { get; set; }
    public int? Simulator { get; set; }
    public int? Factory { get; set; }
    public int? Correlation { get; set; }
}

internal sealed class FinancesJson
{
    public long? Balance { get; set; }
    public long? SeasonBudget { get; set; }
    public long? CostCap { get; set; }
}

internal sealed class DriverJson
{
    public string? Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public int? Age { get; set; }
    public string? Nationality { get; set; }
    public int? Number { get; set; }
    public AttributesJson? Attributes { get; set; }
    public int? Morale { get; set; }
    public int? Reputation { get; set; }
    public CareerJson? Career { get; set; }
}

internal sealed class AttributesJson
{
    public int? Pace { get; set; }
    public int? Racecraft { get; set; }
    public int? Consistency { get; set; }
    public int? TyreManagement { get; set; }
    public int? WetWeather { get; set; }
    public int? Feedback { get; set; }
}

internal sealed class CareerJson
{
    public int? Races { get; set; }
    public int? Wins { get; set; }
    public int? Podiums { get; set; }
    public int? Poles { get; set; }
    public int? FastestLaps { get; set; }
    public int? Championships { get; set; }
    public double? Points { get; set; }
}
