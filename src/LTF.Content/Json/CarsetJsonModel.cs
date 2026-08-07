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
    public List<TestDayJson>? TestDays { get; set; }
    public List<TeamJson>? Teams { get; set; }
    public List<DriverJson>? Drivers { get; set; }
    public List<DriverJson>? Reserves { get; set; }

    /// <summary>Optional regulation era + tuning (ADR-0018). Absent → the DRS era.</summary>
    public RegulationsJson? Regulations { get; set; }

    /// <summary>Optional active contracts binding drivers/staff to teams (M12).</summary>
    public List<ContractJson>? Contracts { get; set; }

    /// <summary>Optional series-wide R&D development catalog (M14).</summary>
    public TechTreeJson? TechTree { get; set; }

    /// <summary>Optional free-agent staff pool available to hire (M14).</summary>
    public List<StaffJson>? StaffPool { get; set; }

    /// <summary>Optional id of the team the player runs (M17). Absent → no player team (all-AI).</summary>
    public string? PlayerTeamId { get; set; }

    /// <summary>Optional per-team boards, ownership and pressure (M17 / ADR-0025).</summary>
    public List<BoardJson>? Boards { get; set; }

    /// <summary>Optional proposed regulation changes the teams vote on (M17 / ADR-0010).</summary>
    public List<RegulationProposalJson>? RegulationProposals { get; set; }
}

internal sealed class RegulationProposalJson
{
    public string? Id { get; set; }
    public string? Description { get; set; }
    public string? FavoredAxis { get; set; }
    public int? Magnitude { get; set; }
}

internal sealed class BoardJson
{
    public string? TeamId { get; set; }
    public string? Ownership { get; set; }
    public List<BoardMemberJson>? Members { get; set; }
    public PressureMetricsJson? Pressure { get; set; }
    public List<ObjectiveJson>? Objectives { get; set; }
    public int? FiringRisk { get; set; }
}

internal sealed class BoardMemberJson
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public int? SportingPriority { get; set; }
    public int? FinancialPriority { get; set; }
    public int? LongTermPriority { get; set; }
    public int? BrandPriority { get; set; }
    public int? DriverDevPriority { get; set; }
    public int? ConfidenceInPlayer { get; set; }
    public int? RiskTolerance { get; set; }
    public List<string>? Traits { get; set; }
}

internal sealed class PressureMetricsJson
{
    public int? BoardConfidence { get; set; }
    public int? SportingPressure { get; set; }
    public int? FinancialPressure { get; set; }
    public int? SponsorPressure { get; set; }
    public int? MediaPressure { get; set; }
    public int? InternalPressure { get; set; }
}

internal sealed class ObjectiveJson
{
    public string? Kind { get; set; }
    public string? Visibility { get; set; }
    public int? Target { get; set; }
    public long? LinkedBudget { get; set; }
    public int? LinkedRisk { get; set; }
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
    public List<AxisFreezeJson>? DevelopmentFreezes { get; set; }
}

internal sealed class AxisFreezeJson
{
    public string? Axis { get; set; }
    public string? Mode { get; set; }
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
    public Dictionary<string, int>? ComponentLifeRounds { get; set; }
    public double? ComponentReliabilityWearInfluence { get; set; }
    public PointsJson? Points { get; set; }
    public EconomyJson? Economy { get; set; }
    public BankJson? Bank { get; set; }
    public ResearchRulesJson? Research { get; set; }
    public DriverDevelopmentJson? DriverDevelopment { get; set; }
    public double? RegulationUnreadinessPenalty { get; set; }
}

internal sealed class DriverDevelopmentJson
{
    public int? PeakAgeStart { get; set; }
    public int? PeakAgeEnd { get; set; }
    public double? GrowthPerSeason { get; set; }
    public double? PhysicalDeclinePerSeason { get; set; }
    public double? ExperienceDeclinePerSeason { get; set; }
    public double? DevelopmentSpread { get; set; }
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

internal sealed class BankJson
{
    public int? BaseRatePercent { get; set; }
    public int? MaxRiskPremiumPercent { get; set; }
    public int? MaxLoanToRevenuePercent { get; set; }
    public int? LatePenaltyPercent { get; set; }
    public int? AssetSeizureAfterMisses { get; set; }
    public int? InsolvencyAfterMisses { get; set; }
    public int? InsolvencyPointsPenalty { get; set; }
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
    public double? TyreGentlenessWearInfluence { get; set; }
    public double? DamageAeroLoss { get; set; }
    public double? DamageRepairSeconds { get; set; }
    public double? ComponentWearPaceLossSeconds { get; set; }
    public double? RefuellingTimeSeconds { get; set; }
    public double? BlockingCoefficient { get; set; }
    public double? EngineWearFactor { get; set; }
    public double? GearboxWearFactor { get; set; }
    public double? BrakeWearFactor { get; set; }
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

internal sealed class TestDayJson
{
    public string? Date { get; set; }
    public string? CircuitId { get; set; }
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
    public ResearchJson? Research { get; set; }
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
    public int? DesignOffice { get; set; }
    public int? WindTunnel { get; set; }
    public int? Cfd { get; set; }
    public int? CompositeManufacturing { get; set; }
    public int? MechanicalWorkshop { get; set; }
    public int? QualityControl { get; set; }
    public int? Simulator { get; set; }
    public int? Dyno { get; set; }
    public int? PitCrewCentre { get; set; }
    public int? DataCentre { get; set; }
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
    public int? Potential { get; set; }
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

internal sealed class TechTreeJson
{
    public List<DepartmentJson>? Departments { get; set; }
    public List<TechNodeJson>? Nodes { get; set; }
}

internal sealed class DepartmentJson
{
    public string? Id { get; set; }
    public string? Name { get; set; }
}

internal sealed class TechNodeJson
{
    public string? Id { get; set; }
    public string? Department { get; set; }
    public string? Category { get; set; }
    public string? Size { get; set; }
    public long? Cost { get; set; }
    public int? Quota { get; set; }
    public List<string>? Prerequisites { get; set; }
    public int? GainMin { get; set; }
    public int? GainMax { get; set; }
    public int? Confidence { get; set; }
    public int? Correlation { get; set; }
}

internal sealed class ResearchJson
{
    public List<string>? UnlockedNodeIds { get; set; }
    public List<ProjectJson>? ActiveProjects { get; set; }
    public ConceptJson? Concept { get; set; }
    public int? RegulationReadiness { get; set; }
}

internal sealed class ProjectJson
{
    public string? NodeId { get; set; }
    public string? State { get; set; }
    public string? TargetAxis { get; set; }
    public int? EstimatedGainMin { get; set; }
    public int? EstimatedGainMax { get; set; }
    public int? Confidence { get; set; }
    public int? CorrelationPercent { get; set; }
    public int? Progress { get; set; }
    public int? RetriesLeft { get; set; }
}

internal sealed class ConceptJson
{
    public int? AeroLean { get; set; }
    public int? PowertrainLean { get; set; }
}

internal sealed class ResearchRulesJson
{
    public int? BaseProgressPerSeason { get; set; }
    public int? StepProgress { get; set; }
    public double? FacilityWeight { get; set; }
    public double? StaffWeight { get; set; }
    public int? CorrelationBaseline { get; set; }
    public int? QuotaPerFacilityLevel { get; set; }
    public int? BaseActiveProjects { get; set; }
    public double? ApproveThreshold { get; set; }
    public int? MaxRetries { get; set; }
    public double? RealizationSpread { get; set; }
    public int? ReadinessGainPerSeason { get; set; }
    public double? QualityControlReliabilityInfluence { get; set; }
    public double? MinorCostMultiplier { get; set; }
    public double? MajorCostMultiplier { get; set; }
    public double? UltimateCostMultiplier { get; set; }
}
