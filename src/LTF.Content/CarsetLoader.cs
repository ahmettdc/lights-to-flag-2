using System.Globalization;
using System.Text.Json;
using LTF.Content.Json;
using LTF.Domain;
using LTF.Domain.Common;
using LTF.Domain.Management;
using LTF.Domain.Racing;
using LTF.Domain.Rnd;

namespace LTF.Content;

/// <summary>
/// Parses a carset.json document into an immutable <see cref="Carset"/>. Pure and
/// I/O-free (it is given the text; the caller reads the file) so it is trivially testable
/// and honours the layering rule in ROADMAP §4.2. Structural problems throw a
/// <see cref="CarsetValidationException"/> with a precise field path; semantic checks
/// (cross-references, counts) live in <see cref="CarsetValidator"/>.
/// </summary>
public static class CarsetLoader
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>Parse and structurally map a carset.json string.</summary>
    public static Carset LoadFromJson(string json)
    {
        CarsetJson? dto;
        try
        {
            dto = JsonSerializer.Deserialize<CarsetJson>(json, Options);
        }
        catch (JsonException ex)
        {
            throw new CarsetValidationException($"carset.json is not valid JSON: {ex.Message}");
        }

        if (dto is null)
        {
            throw new CarsetValidationException("carset.json is empty.");
        }

        return Map(dto);
    }

    /// <summary>Load, then run semantic validation and throw if any error was found.</summary>
    public static Carset LoadAndValidate(string json)
    {
        var carset = LoadFromJson(json);
        var errors = CarsetValidator.Validate(carset)
            .Where(i => i.Severity == ValidationSeverity.Error)
            .ToList();

        if (errors.Count > 0)
        {
            var detail = string.Join("\n", errors.Select(e => "  - " + e.Message));
            throw new CarsetValidationException("Carset has validation errors:\n" + detail);
        }

        return carset;
    }

    // --- Mapping ---

    private static Carset Map(CarsetJson j) => new()
    {
        Id = ReqStr(j.Id, "id"),
        Name = ReqStr(j.Name, "name"),
        SchemaVersion = j.SchemaVersion ?? 1,
        Rules = MapRules(j.Rules),
        Balance = MapBalance(j.Balance),
        Regulations = MapRegulations(j.Regulations),
        Circuits = MapList(j.Circuits, "circuits", MapCircuit),
        Calendar = MapList(j.Calendar, "calendar", MapRound),
        TestDays = MapOptional(j.TestDays, "testDays", MapTestDay),
        Teams = MapList(j.Teams, "teams", MapTeam),
        Drivers = MapList(j.Drivers, "drivers", MapDriver),
        Tyres = MapOptional(j.Tyres, "tyres", MapTyre),
        Reserves = MapOptional(j.Reserves, "reserves", MapDriver),
        Contracts = MapOptional(j.Contracts, "contracts", MapContract),
        TechTree = MapTechTree(j.TechTree),
        StaffPool = MapOptional(j.StaffPool, "staffPool", MapStaff),
        PlayerTeamId = j.PlayerTeamId ?? "",
        Boards = MapOptional(j.Boards, "boards", MapBoard),
        RegulationProposals = MapOptional(j.RegulationProposals, "regulationProposals", MapRegulationProposal),
    };

    private static RulesSet MapRules(RulesJson? r)
    {
        if (r is null)
        {
            throw Err("rules", "is required");
        }

        var points = r.Points ?? throw Err("rules.points", "is required");
        var race = points.RacePoints;
        if (race is null || race.Count == 0)
        {
            throw Err("rules.points.racePoints", "must list at least one score");
        }

        var allocation = new Dictionary<ComponentKind, int>();
        if (r.ComponentAllocation is not null)
        {
            foreach (var (key, value) in r.ComponentAllocation)
            {
                if (!Enum.TryParse<ComponentKind>(key, ignoreCase: true, out var kind))
                {
                    throw Err("rules.componentAllocation", $"has unknown component '{key}'");
                }

                allocation[kind] = value;
            }
        }

        var lifeRounds = new Dictionary<ComponentKind, int>();
        if (r.ComponentLifeRounds is not null)
        {
            foreach (var (key, value) in r.ComponentLifeRounds)
            {
                if (!Enum.TryParse<ComponentKind>(key, ignoreCase: true, out var kind))
                {
                    throw Err("rules.componentLifeRounds", $"has unknown component '{key}'");
                }

                lifeRounds[kind] = value;
            }
        }

        return new RulesSet
        {
            SeriesName = ReqStr(r.SeriesName, "rules.seriesName"),
            Points = new PointsScheme
            {
                RacePoints = race.ToArray(),
                SprintPoints = points.SprintPoints?.ToArray() ?? [],
                PolePoint = points.PolePoint ?? 0,
                FastestLapPoint = points.FastestLapPoint ?? 0,
                LeadingLapPoint = points.LeadingLapPoint ?? 0,
                MostLapsLedPoint = points.MostLapsLedPoint ?? 0,
            },
            Qualifying = EnumOr(r.Qualifying, QualifyingFormat.Knockout),
            RetirementAge = r.RetirementAge ?? 40,
            RefuellingAllowed = r.RefuellingAllowed ?? false,
            MandatoryPitStops = r.MandatoryPitStops ?? 0,
            BothDryCompoundsRequired = r.BothDryCompoundsRequired ?? false,
            DriversUnlapUnderSafetyCar = r.DriversUnlapUnderSafetyCar ?? true,
            ComponentAllocation = allocation,
            GridPenaltyPerExtraComponent = r.GridPenaltyPerExtraComponent ?? 5,
            ComponentLifeRounds = lifeRounds,
            ComponentReliabilityWearInfluence = r.ComponentReliabilityWearInfluence ?? 0,
            Economy = MapEconomy(r.Economy),
            Research = MapResearchRules(r.Research),
        };
    }

    private static EconomyRules MapEconomy(EconomyJson? e)
    {
        if (e is null)
        {
            return new EconomyRules();
        }

        return new EconomyRules
        {
            PrizeMoney = e.PrizeMoney?.ToArray() ?? [],
            TvIncome = e.TvIncome ?? 0,
            OperatingCostPerRace = e.OperatingCostPerRace ?? 0,
            CrashCostPerIncident = e.CrashCostPerIncident ?? 0,
            CostCapFinePercent = e.CostCapFinePercent ?? 0,
            CostCapPointsPerOverage = e.CostCapPointsPerOverage ?? 0,
        };
    }

    private static BalanceCoefficients MapBalance(BalanceJson? b)
    {
        var d = new BalanceCoefficients();
        if (b is null)
        {
            return d;
        }

        return d with
        {
            DriverPaceWeight = b.DriverPaceWeight ?? d.DriverPaceWeight,
            CarPaceWeight = b.CarPaceWeight ?? d.CarPaceWeight,
            RandomnessSpreadSeconds = b.RandomnessSpreadSeconds ?? d.RandomnessSpreadSeconds,
            TyreWearPerLap = b.TyreWearPerLap ?? d.TyreWearPerLap,
            ReliabilityFailureRate = b.ReliabilityFailureRate ?? d.ReliabilityFailureRate,
            SafetyCarBaseChance = b.SafetyCarBaseChance ?? d.SafetyCarBaseChance,
            OvertakeBaseChance = b.OvertakeBaseChance ?? d.OvertakeBaseChance,
            WetPaceLoss = b.WetPaceLoss ?? d.WetPaceLoss,
            FuelLoadPenaltySeconds = b.FuelLoadPenaltySeconds ?? d.FuelLoadPenaltySeconds,
            TyreGentlenessWearInfluence = b.TyreGentlenessWearInfluence ?? d.TyreGentlenessWearInfluence,
        };
    }

    private static RegulationSet MapRegulations(RegulationsJson? r)
    {
        if (r is null)
        {
            return RegulationSet.Drs;
        }

        // Absent era → DRS; an unknown era string is a structural error (mirrors other enums).
        var era = r.Era is null
            ? RegulationEra.DrsEra
            : ReqEnum<RegulationEra>(r.Era, "regulations.era");

        var d = new RegulationSet { Era = era };
        return d with
        {
            EnergyRegenPerLap = r.EnergyRegenPerLap ?? d.EnergyRegenPerLap,
            ManualOverrideEnergyCost = r.ManualOverrideEnergyCost ?? d.ManualOverrideEnergyCost,
            ManualOverrideBoost = r.ManualOverrideBoost ?? d.ManualOverrideBoost,
            DeRatingThreshold = r.DeRatingThreshold ?? d.DeRatingThreshold,
            DeRatingPenaltySeconds = r.DeRatingPenaltySeconds ?? d.DeRatingPenaltySeconds,
            LowDragLapGainSeconds = r.LowDragLapGainSeconds ?? d.LowDragLapGainSeconds,
            HighDownforceLapGainSeconds = r.HighDownforceLapGainSeconds ?? d.HighDownforceLapGainSeconds,
            LowDragTopSpeedKph = r.LowDragTopSpeedKph ?? d.LowDragTopSpeedKph,
        };
    }

    private static TyreSpec MapTyre(TyreJson t, string p) => new()
    {
        Compound = ReqEnum<TyreCompound>(t.Compound, $"{p}.compound"),
        Code = t.Code ?? "",
        Grip = Rate(t.Grip, $"{p}.grip"),
        Durability = Rate(t.Durability, $"{p}.durability"),
    };

    private static Circuit MapCircuit(CircuitJson c, string p) => new()
    {
        Id = ReqStr(c.Id, $"{p}.id"),
        Name = ReqStr(c.Name, $"{p}.name"),
        Country = c.Country ?? "",
        Kind = EnumOr(c.Kind, TrackKind.RoadCourse),
        Laps = ReqInt(c.Laps, $"{p}.laps"),
        LapDistanceKm = ReqDouble(c.LapDistanceKm, $"{p}.lapDistanceKm"),
        BaseLapTimeSeconds = ReqDouble(c.BaseLapTimeSeconds, $"{p}.baseLapTimeSeconds"),
        TyreStress = RateOr(c.TyreStress, 50),
        Overtaking = RateOr(c.Overtaking, 50),
        PowerSensitivity = RateOr(c.PowerSensitivity, 50),
        DownforceSensitivity = RateOr(c.DownforceSensitivity, 50),
        SafetyCarLikelihood = RateOr(c.SafetyCarLikelihood, 50),
        WeatherVariability = RateOr(c.WeatherVariability, 50),
    };

    private static CalendarRound MapRound(CalendarRoundJson r, string p) => new()
    {
        Round = ReqInt(r.Round, $"{p}.round"),
        CircuitId = ReqStr(r.CircuitId, $"{p}.circuitId"),
        Date = ReqDate(r.Date, $"{p}.date"),
        IsSprint = r.Sprint ?? false,
    };

    private static TestDay MapTestDay(TestDayJson t, string p) => new()
    {
        Date = ReqDate(t.Date, $"{p}.date"),
        CircuitId = ReqStr(t.CircuitId, $"{p}.circuitId"),
    };

    private static Team MapTeam(TeamJson t, string p) => new()
    {
        Id = ReqStr(t.Id, $"{p}.id"),
        Name = ReqStr(t.Name, $"{p}.name"),
        ShortName = t.ShortName ?? "",
        Base = t.Base ?? "",
        Principal = t.Principal ?? "",
        Nationality = t.Nationality ?? "",
        Class = t.Class ?? "",
        Car = MapCar(t.Car, $"{p}.car"),
        DriverIds = t.DriverIds?.ToArray() ?? [],
        Facilities = MapFacilities(t.Facilities),
        Finances = MapFinances(t.Finances),
        Sponsors = MapOptional(t.Sponsors, $"{p}.sponsors", MapSponsor),
        Staff = MapOptional(t.Staff, $"{p}.staff", MapStaff),
        Research = MapResearch(t.Research),
        ChampionshipsWon = t.ChampionshipsWon ?? 0,
        RaceWins = t.RaceWins ?? 0,
    };

    private static Car MapCar(CarJson? c, string p)
    {
        if (c is null)
        {
            throw Err(p, "is required");
        }

        return new Car
        {
            Aerodynamics = Rate(c.Aerodynamics, $"{p}.aerodynamics"),
            Chassis = Rate(c.Chassis, $"{p}.chassis"),
            PowerUnit = Rate(c.PowerUnit, $"{p}.powerUnit"),
            TyreGentleness = Rate(c.TyreGentleness, $"{p}.tyreGentleness"),
            Reliability = Rate(c.Reliability, $"{p}.reliability"),
            EngineSupplier = c.EngineSupplier ?? "",
        };
    }

    private static Facilities MapFacilities(FacilitiesJson? f)
    {
        if (f is null)
        {
            return Facilities.Default;
        }

        return new Facilities
        {
            DesignOffice = Level(f.DesignOffice),
            WindTunnel = Level(f.WindTunnel),
            Cfd = Level(f.Cfd),
            CompositeManufacturing = Level(f.CompositeManufacturing),
            MechanicalWorkshop = Level(f.MechanicalWorkshop),
            QualityControl = Level(f.QualityControl),
            Simulator = Level(f.Simulator),
            Dyno = Level(f.Dyno),
            PitCrewCentre = Level(f.PitCrewCentre),
            DataCentre = Level(f.DataCentre),
        };
    }

    private static FacilityLevel Level(int? value) =>
        value is null ? new FacilityLevel(3) : FacilityLevel.Clamped(value.Value);

    private static TechTree MapTechTree(TechTreeJson? t)
    {
        if (t is null)
        {
            return TechTree.Empty;
        }

        return new TechTree
        {
            Departments = MapOptional(t.Departments, "techTree.departments", MapDepartment),
            Nodes = MapOptional(t.Nodes, "techTree.nodes", MapTechNode),
        };
    }

    private static Department MapDepartment(DepartmentJson d, string p) => new()
    {
        Id = ReqStr(d.Id, $"{p}.id"),
        Name = ReqStr(d.Name, $"{p}.name"),
    };

    private static TechNode MapTechNode(TechNodeJson n, string p) => new()
    {
        Id = ReqStr(n.Id, $"{p}.id"),
        DepartmentId = ReqStr(n.Department, $"{p}.department"),
        Category = ReqEnum<CarAxis>(n.Category, $"{p}.category"),
        Size = EnumOr(n.Size, NodeSize.Minor),
        Cost = n.Cost ?? 0,
        Quota = n.Quota ?? 0,
        Prerequisites = n.Prerequisites?.ToArray() ?? [],
        GainMin = n.GainMin ?? 0,
        GainMax = n.GainMax ?? 0,
        Confidence = n.Confidence ?? 100,
        Correlation = n.Correlation ?? 100,
    };

    private static ResearchState MapResearch(ResearchJson? r)
    {
        if (r is null)
        {
            return ResearchState.Empty;
        }

        return new ResearchState
        {
            UnlockedNodeIds = r.UnlockedNodeIds?.ToArray() ?? [],
            ActiveProjects = MapOptional(r.ActiveProjects, "research.activeProjects", MapProject),
            Concept = MapConcept(r.Concept),
            RegulationReadiness = r.RegulationReadiness ?? 0,
        };
    }

    private static DevelopmentProject MapProject(ProjectJson pj, string p) => new()
    {
        NodeId = ReqStr(pj.NodeId, $"{p}.nodeId"),
        State = EnumOr(pj.State, ValidationState.InDesign),
        TargetAxis = EnumOr(pj.TargetAxis, CarAxis.AeroLowSpeed),
        EstimatedGainMin = pj.EstimatedGainMin ?? 0,
        EstimatedGainMax = pj.EstimatedGainMax ?? 0,
        Confidence = pj.Confidence ?? 100,
        CorrelationPercent = pj.CorrelationPercent ?? 100,
        Progress = pj.Progress ?? 0,
        RetriesLeft = pj.RetriesLeft ?? 0,
    };

    private static ConceptDirection MapConcept(ConceptJson? c)
    {
        if (c is null)
        {
            return ConceptDirection.Neutral;
        }

        return new ConceptDirection
        {
            AeroLean = c.AeroLean ?? 0,
            PowertrainLean = c.PowertrainLean ?? 0,
        };
    }

    private static ResearchRules MapResearchRules(ResearchRulesJson? r)
    {
        if (r is null)
        {
            return new ResearchRules();
        }

        var d = new ResearchRules();
        return d with
        {
            BaseProgressPerSeason = r.BaseProgressPerSeason ?? d.BaseProgressPerSeason,
            StepProgress = r.StepProgress ?? d.StepProgress,
            FacilityWeight = r.FacilityWeight ?? d.FacilityWeight,
            StaffWeight = r.StaffWeight ?? d.StaffWeight,
            CorrelationBaseline = r.CorrelationBaseline ?? d.CorrelationBaseline,
            QuotaPerFacilityLevel = r.QuotaPerFacilityLevel ?? d.QuotaPerFacilityLevel,
            BaseActiveProjects = r.BaseActiveProjects ?? d.BaseActiveProjects,
            ApproveThreshold = r.ApproveThreshold ?? d.ApproveThreshold,
            MaxRetries = r.MaxRetries ?? d.MaxRetries,
            RealizationSpread = r.RealizationSpread ?? d.RealizationSpread,
            ReadinessGainPerSeason = r.ReadinessGainPerSeason ?? d.ReadinessGainPerSeason,
            QualityControlReliabilityInfluence =
                r.QualityControlReliabilityInfluence ?? d.QualityControlReliabilityInfluence,
            MinorCostMultiplier = r.MinorCostMultiplier ?? d.MinorCostMultiplier,
            MajorCostMultiplier = r.MajorCostMultiplier ?? d.MajorCostMultiplier,
            UltimateCostMultiplier = r.UltimateCostMultiplier ?? d.UltimateCostMultiplier,
        };
    }

    private static Finances MapFinances(FinancesJson? f)
    {
        if (f is null)
        {
            return new Finances();
        }

        return new Finances
        {
            Balance = f.Balance ?? 0,
            SeasonBudget = f.SeasonBudget ?? 0,
            CostCap = f.CostCap ?? 0,
        };
    }

    private static Sponsor MapSponsor(SponsorJson s, string p) => new()
    {
        Id = ReqStr(s.Id, $"{p}.id"),
        Name = ReqStr(s.Name, $"{p}.name"),
        Tier = EnumOr(s.Tier, SponsorTier.Secondary),
        PerRaceFee = s.PerRaceFee ?? 0,
        PerPointBonus = s.PerPointBonus ?? 0,
        ObjectiveBonus = s.ObjectiveBonus ?? 0,
        ObjectivePosition = s.ObjectivePosition ?? 0,
    };

    private static Staff MapStaff(StaffJson s, string p) => new()
    {
        Id = ReqStr(s.Id, $"{p}.id"),
        FirstName = ReqStr(s.FirstName, $"{p}.firstName"),
        LastName = ReqStr(s.LastName, $"{p}.lastName"),
        Role = ReqEnum<StaffRole>(s.Role, $"{p}.role"),
        Skill = Rate(s.Skill, $"{p}.skill"),
        Nationality = s.Nationality ?? "",
        Age = s.Age ?? 0,
        Salary = s.Salary ?? 0,
    };

    private static Driver MapDriver(DriverJson d, string p)
    {
        var a = d.Attributes ?? throw Err($"{p}.attributes", "is required");

        return new Driver
        {
            Id = ReqStr(d.Id, $"{p}.id"),
            FirstName = ReqStr(d.FirstName, $"{p}.firstName"),
            LastName = ReqStr(d.LastName, $"{p}.lastName"),
            Age = ReqInt(d.Age, $"{p}.age"),
            Nationality = d.Nationality ?? "",
            Number = d.Number ?? 0,
            Attributes = new DriverAttributes
            {
                Pace = Rate(a.Pace, $"{p}.attributes.pace"),
                Racecraft = Rate(a.Racecraft, $"{p}.attributes.racecraft"),
                Consistency = Rate(a.Consistency, $"{p}.attributes.consistency"),
                TyreManagement = Rate(a.TyreManagement, $"{p}.attributes.tyreManagement"),
                WetWeather = Rate(a.WetWeather, $"{p}.attributes.wetWeather"),
                Feedback = Rate(a.Feedback, $"{p}.attributes.feedback"),
            },
            Morale = RateOr(d.Morale, 50),
            Reputation = RateOr(d.Reputation, 50),
            Career = MapCareer(d.Career),
            Personality = MapPersonality(d.Personality),
        };
    }

    private static Personality MapPersonality(PersonalityJson? p)
    {
        if (p is null)
        {
            return Personality.Neutral;
        }

        return new Personality
        {
            Ego = RateOr(p.Ego, 50),
            Loyalty = RateOr(p.Loyalty, 50),
            Temperament = RateOr(p.Temperament, 50),
            Ambition = RateOr(p.Ambition, 50),
        };
    }

    private static Contract MapContract(ContractJson c, string p) => new()
    {
        Kind = EnumOr(c.Kind, ContractKind.Driver),
        PartyId = ReqStr(c.PartyId, $"{p}.partyId"),
        TeamId = ReqStr(c.TeamId, $"{p}.teamId"),
        SalaryPerSeason = c.SalaryPerSeason ?? 0,
        SeasonsRemaining = c.SeasonsRemaining ?? 1,
        SigningBonus = c.SigningBonus ?? 0,
        Clauses = MapClauses(c.Clauses),
    };

    private static ContractClauses MapClauses(ContractClausesJson? c)
    {
        if (c is null)
        {
            return new ContractClauses();
        }

        return new ContractClauses
        {
            PerPointBonus = c.PerPointBonus ?? 0,
            ChampionshipBonus = c.ChampionshipBonus ?? 0,
            ExitClause = c.ExitClause ?? 0,
            FirstDriverStatus = c.FirstDriverStatus ?? false,
        };
    }

    private static DriverCareer MapCareer(CareerJson? c)
    {
        if (c is null)
        {
            return DriverCareer.None;
        }

        return new DriverCareer
        {
            Races = c.Races ?? 0,
            Wins = c.Wins ?? 0,
            Podiums = c.Podiums ?? 0,
            Poles = c.Poles ?? 0,
            FastestLaps = c.FastestLaps ?? 0,
            Championships = c.Championships ?? 0,
            Points = c.Points ?? 0,
        };
    }

    private static TeamBoard MapBoard(BoardJson b, string p) => new()
    {
        TeamId = ReqStr(b.TeamId, $"{p}.teamId"),
        Ownership = EnumOr(b.Ownership, OwnershipType.RacingOwner),
        Members = MapOptional(b.Members, $"{p}.members", MapBoardMember),
        Metrics = MapPressureMetrics(b.Pressure),
        Objectives = MapOptional(b.Objectives, $"{p}.objectives", MapObjective),
        FiringRisk = PressOr(b.FiringRisk, 0),
    };

    private static BoardMember MapBoardMember(BoardMemberJson m, string p) => new()
    {
        Id = ReqStr(m.Id, $"{p}.id"),
        Name = m.Name ?? "",
        SportingPriority = RateOr(m.SportingPriority, 50),
        FinancialPriority = RateOr(m.FinancialPriority, 50),
        LongTermPriority = RateOr(m.LongTermPriority, 50),
        BrandPriority = RateOr(m.BrandPriority, 50),
        DriverDevPriority = RateOr(m.DriverDevPriority, 50),
        ConfidenceInPlayer = PressOr(m.ConfidenceInPlayer, 50),
        RiskTolerance = RateOr(m.RiskTolerance, 50),
        Traits = MapTraits(m.Traits, p),
    };

    private static PressureMetrics MapPressureMetrics(PressureMetricsJson? m)
    {
        if (m is null)
        {
            return PressureMetrics.Neutral;
        }

        return new PressureMetrics
        {
            BoardConfidence = PressOr(m.BoardConfidence, 50),
            SportingPressure = PressOr(m.SportingPressure, 50),
            FinancialPressure = PressOr(m.FinancialPressure, 50),
            SponsorPressure = PressOr(m.SponsorPressure, 50),
            MediaPressure = PressOr(m.MediaPressure, 50),
            InternalPressure = PressOr(m.InternalPressure, 50),
        };
    }

    private static Objective MapObjective(ObjectiveJson o, string p) => new()
    {
        Kind = ReqEnum<ObjectiveKind>(o.Kind, $"{p}.kind"),
        Visibility = EnumOr(o.Visibility, ObjectiveVisibility.Open),
        Target = o.Target ?? 0,
        LinkedBudget = o.LinkedBudget ?? 0,
        LinkedRisk = o.LinkedRisk ?? 0,
    };

    private static BoardMemberTraits MapTraits(List<string>? traits, string p)
    {
        if (traits is null)
        {
            return BoardMemberTraits.None;
        }

        var result = BoardMemberTraits.None;
        foreach (var trait in traits)
        {
            result |= ReqEnum<BoardMemberTraits>(trait, $"{p}.traits");
        }

        return result;
    }

    private static RegulationProposal MapRegulationProposal(RegulationProposalJson r, string p) => new()
    {
        Id = ReqStr(r.Id, $"{p}.id"),
        Description = r.Description ?? "",
        FavoredAxis = ReqEnum<CarAxis>(r.FavoredAxis, $"{p}.favoredAxis"),
        Magnitude = r.Magnitude ?? 0,
    };

    // --- Helpers ---

    private static IReadOnlyList<T> MapList<TJson, T>(
        List<TJson>? list, string field, Func<TJson, string, T> map)
    {
        if (list is null || list.Count == 0)
        {
            throw Err(field, "must have at least one entry");
        }

        return MapEach(list, field, map);
    }

    private static IReadOnlyList<T> MapOptional<TJson, T>(
        List<TJson>? list, string field, Func<TJson, string, T> map) =>
        list is null ? [] : MapEach(list, field, map);

    private static T[] MapEach<TJson, T>(List<TJson> list, string field, Func<TJson, string, T> map)
    {
        var result = new T[list.Count];
        for (var i = 0; i < list.Count; i++)
        {
            result[i] = map(list[i], $"{field}[{i}]");
        }

        return result;
    }

    private static string ReqStr(string? value, string field) =>
        string.IsNullOrWhiteSpace(value) ? throw Err(field, "is required") : value;

    private static int ReqInt(int? value, string field) =>
        value ?? throw Err(field, "is required");

    private static double ReqDouble(double? value, string field) =>
        value ?? throw Err(field, "is required");

    private static Rating Rate(int? value, string field)
    {
        var n = value ?? throw Err(field, "is required");
        if (n < Rating.Min || n > Rating.Max)
        {
            throw Err(field, $"must be {Rating.Min}–{Rating.Max} (was {n})");
        }

        return new Rating(n);
    }

    private static Rating RateOr(int? value, int fallback) =>
        value is null ? new Rating(fallback) : Rating.Clamped(value.Value);

    private static Pressure PressOr(int? value, int fallback) =>
        value is null ? new Pressure(fallback) : Pressure.Clamped(value.Value);

    private static TEnum ReqEnum<TEnum>(string? value, string field) where TEnum : struct, Enum
    {
        var s = ReqStr(value, field);
        return Enum.TryParse<TEnum>(s, ignoreCase: true, out var parsed)
            ? parsed
            : throw Err(field, $"has unknown value '{s}'");
    }

    private static TEnum EnumOr<TEnum>(string? value, TEnum fallback) where TEnum : struct, Enum =>
        value is not null && Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed)
            ? parsed
            : fallback;

    private static DateOnly ReqDate(string? value, string field)
    {
        var s = ReqStr(value, field);
        return DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : throw Err(field, $"must be a date 'yyyy-MM-dd' (was '{s}')");
    }

    private static CarsetValidationException Err(string field, string reason) =>
        new($"{field} {reason}.");
}
