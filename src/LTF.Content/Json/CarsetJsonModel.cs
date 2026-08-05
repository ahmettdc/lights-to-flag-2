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

    /// <summary>Optional regulation era + tuning (ADR-0018). Absent → the DRS era.</summary>
    public RegulationsJson? Regulations { get; set; }

    /// <summary>Optional active contracts binding drivers/staff to teams (M12).</summary>
    public List<ContractJson>? Contracts { get; set; }
}

internal sealed class RegulationsJson
{
    public string? Era { get; set; }
    public double? EnergyRegenPerLap { get; set; }
    public double? ManualOverrideEnergyCost { get; set; }
    public double? ManualOverrideBoost { get; set; }
    public double? DeRatingThreshold { get; set; }
    public double? DeRatingPenaltySeconds { get; set; }
    public double? LowDragLapGainSeconds { get; set; }
    public double? HighDownforceLapGainSeconds { get; set; }
    public double? LowDragTopSpeedKph { get; set; }
}

internal sealed class RulesJson
{
    public string? SeriesName { get; set; }
    public string? Qualifying { get; set; }
    public int? RetirementAge { get; set; }
    public bool? RefuellingAllowed { get; set; }
    public int? MandatoryPitStops { get; set; }
    public bool? BothDryCompoundsRequired { get; set; }
    public bool? DriversUnlapUnderSafetyCar { get; set; }
    public int? GridPenaltyPerExtraComponent { get; set; }
    public Dictionary<string, int>? ComponentAllocation { get; set; }
    public PointsJson? Points { get; set; }
    public EconomyJson? Economy { get; set; }
}

internal sealed class PointsJson
{
    public List<int>? RacePoints { get; set; }
    public List<int>? SprintPoints { get; set; }
    public int? PolePoint { get; set; }
    public int? FastestLapPoint { get; set; }
    public int? LeadingLapPoint { get; set; }
    public int? MostLapsLedPoint { get; set; }
}

internal sealed class EconomyJson
{
    public List<long>? PrizeMoney { get; set; }
    public long? TvIncome { get; set; }
    public long? OperatingCostPerRace { get; set; }
    public long? CrashCostPerIncident { get; set; }
    public int? CostCapFinePercent { get; set; }
    public long? CostCapPointsPerOverage { get; set; }
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
    public string? Class { get; set; }
    public CarJson? Car { get; set; }
    public List<string>? DriverIds { get; set; }
    public FacilitiesJson? Facilities { get; set; }
    public FinancesJson? Finances { get; set; }
    public List<SponsorJson>? Sponsors { get; set; }
    public List<StaffJson>? Staff { get; set; }
    public int? ChampionshipsWon { get; set; }
    public int? RaceWins { get; set; }
}

internal sealed class SponsorJson
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Tier { get; set; }
    public long? PerRaceFee { get; set; }
    public long? PerPointBonus { get; set; }
    public long? ObjectiveBonus { get; set; }
    public int? ObjectivePosition { get; set; }
}

internal sealed class StaffJson
{
    public string? Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Role { get; set; }
    public int? Skill { get; set; }
    public string? Nationality { get; set; }
    public int? Age { get; set; }
    public long? Salary { get; set; }
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
    public PersonalityJson? Personality { get; set; }
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

internal sealed class PersonalityJson
{
    public int? Ego { get; set; }
    public int? Loyalty { get; set; }
    public int? Temperament { get; set; }
    public int? Ambition { get; set; }
}

internal sealed class ContractJson
{
    public string? Kind { get; set; }
    public string? PartyId { get; set; }
    public string? TeamId { get; set; }
    public long? SalaryPerSeason { get; set; }
    public int? SeasonsRemaining { get; set; }
    public long? SigningBonus { get; set; }
    public ContractClausesJson? Clauses { get; set; }
}

internal sealed class ContractClausesJson
{
    public long? PerPointBonus { get; set; }
    public long? ChampionshipBonus { get; set; }
    public long? ExitClause { get; set; }
    public bool? FirstDriverStatus { get; set; }
}
