using System.Globalization;
using System.Text.Json;
using LTF.Content.Json;
using LTF.Domain;
using LTF.Domain.Common;
using LTF.Domain.Management;
using LTF.Domain.Racing;

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
        Teams = MapList(j.Teams, "teams", MapTeam),
        Drivers = MapList(j.Drivers, "drivers", MapDriver),
        Tyres = MapOptional(j.Tyres, "tyres", MapTyre),
        Reserves = MapOptional(j.Reserves, "reserves", MapDriver),
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
            ComponentAllocation = allocation,
            GridPenaltyPerExtraComponent = r.GridPenaltyPerExtraComponent ?? 5,
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

    private static Team MapTeam(TeamJson t, string p) => new()
    {
        Id = ReqStr(t.Id, $"{p}.id"),
        Name = ReqStr(t.Name, $"{p}.name"),
        ShortName = t.ShortName ?? "",
        Base = t.Base ?? "",
        Principal = t.Principal ?? "",
        Nationality = t.Nationality ?? "",
        Car = MapCar(t.Car, $"{p}.car"),
        DriverIds = t.DriverIds?.ToArray() ?? [],
        Facilities = MapFacilities(t.Facilities),
        Finances = MapFinances(t.Finances),
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
            WindTunnel = RateOr(f.WindTunnel, 50),
            Simulator = RateOr(f.Simulator, 50),
            Factory = RateOr(f.Factory, 50),
            Correlation = RateOr(f.Correlation, 50),
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
