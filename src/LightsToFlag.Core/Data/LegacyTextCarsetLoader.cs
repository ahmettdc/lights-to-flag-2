using LightsToFlag.Core.Domain;

namespace LightsToFlag.Core.Data;

/// <summary>
/// Loads a carset stored in the original underscore-separated text format
/// (<c>Rules.txt</c>, <c>Coefficients.txt</c>, <c>Teamdata.txt</c>,
/// <c>Driverdata.txt</c>, <c>Circuitdata.txt</c>). Field orders follow the
/// schemas documented in <c>Carsetmaker/*desc.txt</c>. Records are parsed
/// sequentially rather than by the legacy engine's flat-offset arithmetic.
/// </summary>
public sealed class LegacyTextCarsetLoader : ICarsetLoader
{
    private const char FieldSeparator = '_';

    // Circuit record: fixed prefix width before the per-class laptimes.
    private const int CircuitFixedFieldCount = 34;
    private const int CircuitCornerStrings = 6;
    private const int CircuitOvertakingStrings = 3;

    public Carset Load(string carsetFolder)
    {
        if (!Directory.Exists(carsetFolder))
        {
            throw new CarsetValidationException($"Carset folder not found: {carsetFolder}");
        }

        var rules = LoadRules(RequireFile(carsetFolder, "Rules.txt"));
        var coefficients = LoadCoefficients(RequireFile(carsetFolder, "Coefficients.txt"));
        var teams = LoadTeams(RequireFile(carsetFolder, "Teamdata.txt"));
        var (drivers, reserves) = LoadDrivers(RequireFile(carsetFolder, "Driverdata.txt"), rules);
        var circuits = LoadCircuits(RequireFile(carsetFolder, "Circuitdata.txt"), rules.ClassCount);

        ValidateCounts(rules, circuits);

        return new Carset
        {
            Name = new DirectoryInfo(carsetFolder).Name,
            SourcePath = Path.GetFullPath(carsetFolder),
            Rules = rules,
            Coefficients = coefficients,
            Drivers = drivers,
            Reserves = reserves,
            Teams = teams,
            Circuits = circuits,
        };
    }

    private static string RequireFile(string folder, string fileName)
    {
        var path = Path.Combine(folder, fileName);
        if (!File.Exists(path))
        {
            throw new CarsetValidationException($"Missing carset file: {fileName} (looked in {folder})");
        }

        return path;
    }

    private static string[] SplitRecord(string line) => line.Split(FieldSeparator);

    private static IReadOnlyList<string[]> ReadRecords(string path)
    {
        var records = new List<string[]>();
        foreach (var line in File.ReadLines(path))
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                records.Add(SplitRecord(line));
            }
        }

        return records;
    }

    private static RulesSet LoadRules(string path)
    {
        // Rules.txt is a single underscore-separated line.
        var text = File.ReadAllText(path).ReplaceLineEndings(string.Empty);
        var reader = new FieldReader(SplitRecord(text), "Rules.txt");

        var governingBody = reader.ReadString();
        var seriesName = reader.ReadString();
        var championshipName = reader.ReadString();
        var eventTitle = reader.ReadString();
        var carType = ParseCarType(reader.ReadString());
        var driverCount = reader.ReadInt();
        var spareCount = reader.ReadInt();
        var circuitCount = reader.ReadInt();
        var tyresPerCompound = reader.ReadInt();
        var driversDisplayed = reader.ReadInt();
        var classCount = Math.Max(1, reader.ReadInt());
        var classNames = reader.ReadStrings(classCount);

        var retirementAge = reader.ReadInt();
        var refuelling = reader.ReadBool();
        var tyreChanges = reader.ReadBool();
        var qualiOnRaceFuel = reader.ReadBool();
        var qualiOnRaceTyre = reader.ReadBool();
        var bothDryCompounds = reader.ReadBool();
        var raceInRain = reader.ReadBool();
        var sharedPitBox = reader.ReadBool();
        var pitLaneClosesAfterSc = reader.ReadInt();
        var stopGoPenalty = reader.ReadInt();
        var fixedNumbers = reader.ReadBool();

        var pointsForPole = reader.ReadDouble();
        var pointsForFastestLap = reader.ReadDouble();
        var pointsForLeadingLap = reader.ReadDouble();
        var pointsForLeadingMostLaps = reader.ReadDouble();

        var knockout = reader.ReadBool();
        var singleLap = reader.ReadBool();
        var qualiMaxLaps = reader.ReadInt();
        var qualiSessionCount = reader.ReadInt();
        var qualiMinutes = reader.ReadInts(Math.Max(0, qualiSessionCount));
        var rollingStart = reader.ReadBool();

        var primaryCount = reader.ReadInt();
        var primaryPoints = reader.ReadDoubles(Math.Max(0, primaryCount));
        var secondaryCount = reader.ReadInt();
        var secondaryPoints = reader.ReadDoubles(Math.Max(0, secondaryCount));

        var engines = reader.ReadInt();
        var chassis = reader.ReadInt();
        var ballastPlus = reader.ReadInt();
        var ballastMinus = reader.ReadInt();
        var ballastMax = reader.ReadInt();
        var chaseRound = reader.ReadInt();
        var chaseDrivers = reader.ReadInt();
        var chasePoints = reader.ReadDouble();
        var unlapUnderSc = reader.ReadBool();

        return new RulesSet
        {
            GoverningBody = governingBody,
            SeriesName = seriesName,
            ChampionshipName = championshipName,
            EventTitle = eventTitle,
            CarType = carType,
            DriverCount = driverCount,
            SpareCount = spareCount,
            CircuitCount = circuitCount,
            TyresPerCompound = tyresPerCompound,
            DriversDisplayed = driversDisplayed,
            ClassCount = classCount,
            ClassNames = classNames,
            RetirementAge = retirementAge,
            RefuellingAllowed = refuelling,
            TyreChangesAllowed = tyreChanges,
            QualifyingOnRaceFuel = qualiOnRaceFuel,
            QualifyingOnRaceTyre = qualiOnRaceTyre,
            BothDryCompoundsRequired = bothDryCompounds,
            RaceInRain = raceInRain,
            SharedTeamPitBox = sharedPitBox,
            PitLaneClosesAfterScLaps = pitLaneClosesAfterSc,
            StopGoPenaltySeconds = stopGoPenalty,
            FixedDriverNumbers = fixedNumbers,
            PointsForPole = pointsForPole,
            PointsForFastestLap = pointsForFastestLap,
            PointsForLeadingLap = pointsForLeadingLap,
            PointsForLeadingMostLaps = pointsForLeadingMostLaps,
            KnockoutQualifying = knockout,
            SingleLapQualifying = singleLap,
            QualifyingMaxLapsPerSession = qualiMaxLaps,
            QualifyingSessionCount = qualiSessionCount,
            QualifyingSessionMinutes = qualiMinutes,
            RollingStart = rollingStart,
            PrimaryPoints = primaryPoints,
            SecondaryPoints = secondaryPoints,
            EnginesPerSeason = engines,
            ChassisPerSeason = chassis,
            BallastIncreaseForPodium = ballastPlus,
            BallastReductionOtherwise = ballastMinus,
            MaxBallast = ballastMax,
            ChaseRound = chaseRound,
            ChaseDriverCount = chaseDrivers,
            ChasePoints = chasePoints,
            DriversUnlapUnderSafetyCar = unlapUnderSc,
        };
    }

    private static Coefficients LoadCoefficients(string path)
    {
        var text = File.ReadAllText(path).ReplaceLineEndings(string.Empty);
        var reader = new FieldReader(SplitRecord(text), "Coefficients.txt");
        return new Coefficients
        {
            PoorWeatherLikelihood = reader.ReadDouble(),
            WeatherChangeRate = reader.ReadDouble(),
            TrackGripChangeRate = reader.ReadDouble(),
            DriverPaceEffect = reader.ReadDouble(),
            DriverConsistencyEffect = reader.ReadDouble(),
            AeroImportance = reader.ReadDouble(),
            MechanicalGripImportance = reader.ReadDouble(),
            EngineImportance = reader.ReadDouble(),
            TyreWear = reader.ReadDouble(),
            ChassisWear = reader.ReadDouble(),
            EngineWear = reader.ReadDouble(),
            DriverErrorRate = reader.ReadDouble(),
            MechanicalProblemRate = reader.ReadDouble(),
            FuelWeightPenalty = reader.ReadDouble(),
            CollisionRate = reader.ReadDouble(),
            OvertakingRate = reader.ReadDouble(),
            SoftHardGap = reader.ReadDouble(),
            SoftHardWearRatio = reader.ReadDouble(),
            InSeasonDriverUpgradeRate = reader.ReadDouble(),
            InSeasonCarUpgradeRate = reader.ReadDouble(),
            SafetyCarPeriodFactor = reader.ReadDouble(),
            RefuellingTime = reader.ReadDouble(),
            TyreChangeTime = reader.ReadDouble(),
            DamageFixTime = reader.ReadDouble(),
            BlockingCoefficient = reader.ReadDouble(),
            SetupEffectiveness = reader.ReadDouble(),
            SafetyCarLikelihood = reader.ReadDouble(),
        };
    }

    private static IReadOnlyList<TeamRating> LoadTeams(string path)
    {
        var teams = new List<TeamRating>();
        var records = ReadRecords(path);
        for (var i = 0; i < records.Count; i++)
        {
            var reader = new FieldReader(records[i], $"Teamdata.txt line {i + 1}");
            teams.Add(new TeamRating
            {
                TitleSponsor = reader.ReadString(),
                Name = reader.ReadString(),
                EngineBrand = reader.ReadString(),
                ChassisName = reader.ReadString(),
                EngineName = reader.ReadString(),
                Class = Math.Max(1, reader.ReadInt()),
                TyreManufacturer = reader.ReadString(),
                Aerodynamics = reader.ReadInt(),
                MechanicalGrip = reader.ReadInt(),
                Engine = reader.ReadInt(),
                EaseOnTyres = reader.ReadInt(),
                Reliability = reader.ReadInt(),
                WetWeather = reader.ReadInt(),
                Setup = reader.ReadInt(),
                Qualifying = reader.ReadInt(),
                Resources = reader.ReadInt(),
                Base = reader.ReadString(),
                Principal = reader.ReadString(),
                Debut = reader.ReadString(),
                Races = reader.ReadInt(),
                Wins = reader.ReadInt(),
                Poles = reader.ReadInt(),
                FastestLaps = reader.ReadInt(),
                Points = reader.ReadDouble(),
                DriversChampionships = reader.ReadInt(),
                ConstructorsChampionships = reader.ReadInt(),
            });
        }

        return teams;
    }

    private static (IReadOnlyList<DriverRating> Drivers, IReadOnlyList<RookieRating> Reserves) LoadDrivers(
        string path, RulesSet rules)
    {
        var records = ReadRecords(path);
        var expected = rules.DriverCount + rules.SpareCount;
        if (records.Count != expected)
        {
            throw new CarsetValidationException(
                $"Driver count mismatch: Rules expects {rules.DriverCount} drivers + {rules.SpareCount} spares " +
                $"= {expected}, but Driverdata.txt has {records.Count} records.");
        }

        // The first DriverCount records are full 24-field drivers; the trailing
        // SpareCount records use the abbreviated rookie schema.
        var drivers = new List<DriverRating>(rules.DriverCount);
        for (var i = 0; i < rules.DriverCount; i++)
        {
            var reader = new FieldReader(records[i], $"Driverdata.txt line {i + 1}");
            drivers.Add(new DriverRating
            {
                FirstName = reader.ReadString(),
                LastName = reader.ReadString(),
                Age = reader.ReadInt(),
                Number = reader.ReadInt(),
                TeamNumber = reader.ReadInt(),
                NumberWithinTeam = reader.ReadInt(),
                Pace = reader.ReadInt(),
                Consistency = reader.ReadInt(),
                Concentration = reader.ReadInt(),
                WetWeather = reader.ReadInt(),
                Overtaking = reader.ReadInt(),
                Smoothness = reader.ReadInt(),
                Feedback = reader.ReadInt(),
                Teamwork = reader.ReadInt(),
                Qualifying = reader.ReadInt(),
                Nationality = reader.ReadString(),
                Debut = reader.ReadString(),
                Races = reader.ReadInt(),
                Wins = reader.ReadInt(),
                Podiums = reader.ReadInt(),
                Poles = reader.ReadInt(),
                FastestLaps = reader.ReadInt(),
                Championships = reader.ReadInt(),
                CareerPoints = reader.ReadDouble(),
            });
        }

        var reserves = new List<RookieRating>(rules.SpareCount);
        for (var i = rules.DriverCount; i < records.Count; i++)
        {
            var reader = new FieldReader(records[i], $"Driverdata.txt line {i + 1} (reserve)");
            reserves.Add(new RookieRating
            {
                FirstName = reader.ReadString(),
                LastName = reader.ReadString(),
                Age = reader.ReadInt(),
                Nationality = reader.ReadString(),
                Bias = reader.Remaining > 0 ? reader.ReadString() : "",
            });
        }

        return (drivers, reserves);
    }

    private static IReadOnlyList<CircuitSpec> LoadCircuits(string path, int classCount)
    {
        var circuits = new List<CircuitSpec>();
        var records = ReadRecords(path);
        for (var i = 0; i < records.Count; i++)
        {
            var context = $"Circuitdata.txt line {i + 1}";
            var record = records[i];
            var expected = CircuitFixedFieldCount + classCount + CircuitCornerStrings + CircuitOvertakingStrings;
            if (record.Length < expected)
            {
                throw new CarsetValidationException(
                    $"{context}: expected at least {expected} fields for {classCount} class(es) but found {record.Length}.");
            }

            var reader = new FieldReader(record, context);
            circuits.Add(new CircuitSpec
            {
                Name = reader.ReadString(),
                Sponsor = reader.ReadString(),
                Venue = reader.ReadString(),
                Kind = ParseTrackKind(reader.ReadString()),
                BackToBack = reader.ReadBool(),
                ReversedGridPlaces = reader.ReadInt(),
                PointsFormat = Math.Max(1, reader.ReadInt()),
                LapRecord = reader.ReadDouble(),
                LapRecordHolder = reader.ReadString(),
                Laps = reader.ReadInt(),
                Minutes = reader.ReadInt(),
                LapLength = reader.ReadString(),
                RaceLength = reader.ReadString(),
                RacesHeld = reader.ReadInt(),
                Debut = reader.ReadString(),
                TyreWear = reader.ReadInt(),
                Overtaking = reader.ReadInt(),
                SafetyCarLikelihood = reader.ReadInt(),
                WeatherChangeability = reader.ReadInt(),
                TopSpeeds = reader.ReadInt(),
                HighSpeedCorners = reader.ReadInt(),
                LowSpeedCorners = reader.ReadInt(),
                Difficulty = reader.ReadInt(),
                FuelPenaltyPerLap = reader.ReadDouble(),
                RubberInPerLap = reader.ReadDouble(),
                AttritionRate = reader.ReadInt(),
                PitLaneSeconds = reader.ReadInt(),
                RunToFirstCorner = reader.ReadInt(),
                MandatoryPitStops = reader.ReadInt(),
                BallastPenaltyPerKgPerLap = reader.ReadDouble(),
                FuelTankPercent = reader.ReadInt(),
                HalfTyreChangesAllowed = reader.ReadBool(),
                DaybreakMinute = reader.ReadInt(),
                NightfallMinute = reader.ReadInt(),
                BaseLaptimeByClass = reader.ReadDoubles(classCount),
                CornerStrings = reader.ReadStrings(CircuitCornerStrings),
                OvertakingStrings = reader.ReadStrings(CircuitOvertakingStrings),
            });
        }

        return circuits;
    }

    private static void ValidateCounts(
        RulesSet rules,
        IReadOnlyList<CircuitSpec> circuits)
    {
        if (circuits.Count != rules.CircuitCount)
        {
            throw new CarsetValidationException(
                $"Circuit count mismatch: Rules expects {rules.CircuitCount} circuits, " +
                $"but Circuitdata.txt has {circuits.Count} records.");
        }
    }

    private static CarType ParseCarType(string raw) => raw.ToLowerInvariant() switch
    {
        "bike" => CarType.Bike,
        "sports" => CarType.Sports,
        _ => CarType.Single,
    };

    private static TrackKind ParseTrackKind(string raw) =>
        raw.Trim().Equals("oval", StringComparison.OrdinalIgnoreCase) ? TrackKind.Oval : TrackKind.Track;
}
