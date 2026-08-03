using LightsToFlag.Core.Domain;
using LightsToFlag.Core.Simulation;

namespace LightsToFlag.Tests;

/// <summary>Builders for lightweight, tunable simulation inputs used across tests.</summary>
internal static class Fixtures
{
    public static DriverRating Driver(
        string name = "Test Driver",
        int pace = 5,
        int consistency = 5,
        int concentration = 5,
        int wet = 5,
        int overtaking = 5,
        int smoothness = 5,
        int qualifying = 5)
    {
        return new DriverRating
        {
            FirstName = name,
            LastName = "",
            Pace = pace,
            Consistency = consistency,
            Concentration = concentration,
            WetWeather = wet,
            Overtaking = overtaking,
            Smoothness = smoothness,
            Feedback = 5,
            Teamwork = 5,
            Qualifying = qualifying,
            Number = 1,
            TeamNumber = 1,
        };
    }

    public static TeamRating Team(
        string name = "Test Team",
        int aero = 5,
        int mech = 5,
        int engine = 5,
        int easeOnTyres = 5,
        int reliability = 5,
        int setup = 5,
        int qualifying = 5)
    {
        return new TeamRating
        {
            Name = name,
            Aerodynamics = aero,
            MechanicalGrip = mech,
            Engine = engine,
            EaseOnTyres = easeOnTyres,
            Reliability = reliability,
            WetWeather = 5,
            Setup = setup,
            Qualifying = qualifying,
            Resources = 5,
            Class = 1,
        };
    }

    public static CircuitSpec Circuit(
        string name = "Test Circuit",
        int laps = 50,
        double baseLaptime = 90.0,
        int tyreWear = 5,
        int overtaking = 5,
        int scLikelihood = 3,
        int attrition = 3,
        double fuelPenaltyPerLap = 0.13,
        int pitLaneSeconds = 20,
        int mandatoryStops = 1)
    {
        return new CircuitSpec
        {
            Name = name,
            Laps = laps,
            BaseLaptimeByClass = new[] { baseLaptime },
            TyreWear = tyreWear,
            Overtaking = overtaking,
            SafetyCarLikelihood = scLikelihood,
            AttritionRate = attrition,
            FuelPenaltyPerLap = fuelPenaltyPerLap,
            PitLaneSeconds = pitLaneSeconds,
            MandatoryPitStops = mandatoryStops,
            HighSpeedCorners = 5,
            LowSpeedCorners = 5,
            TopSpeeds = 5,
            Difficulty = 5,
            CornerStrings = new[] { "", "", "", "", "", "" },
            OvertakingStrings = new[] { "", "", "" },
        };
    }

    public static Coefficients Coefficients() => new()
    {
        AeroImportance = 0.06,
        MechanicalGripImportance = 0.04,
        EngineImportance = 0.05,
        DriverPaceEffect = 1.0,
        DriverConsistencyEffect = 1.0,
        TyreWear = 0.03,
        MechanicalProblemRate = 0.01,
        DriverErrorRate = 0.01,
        CollisionRate = 0.5,
        OvertakingRate = 0.8,
        SafetyCarLikelihood = 1.0,
        SafetyCarPeriodFactor = 1.05,
        RefuellingTime = 0.0,
        TyreChangeTime = 3.0,
        SetupEffectiveness = 1.0,
        FuelWeightPenalty = 0.5,
    };

    public static RulesSet Rules(int qualifyingMaxLaps = 3) => new()
    {
        GoverningBody = "TEST",
        DriverCount = 20,
        CircuitCount = 1,
        ClassCount = 1,
        QualifyingMaxLapsPerSession = qualifyingMaxLaps,
        PrimaryPoints = new double[] { 25, 18, 15, 12, 10, 8, 6, 4, 2, 1 },
        PointsForFastestLap = 1,
    };

    public static Competitor Competitor(
        string id = "1",
        DriverRating? driver = null,
        TeamRating? team = null,
        int ballastKg = 0,
        double setupQuality = 0.0)
    {
        return new Competitor
        {
            Id = id,
            Driver = driver ?? Driver(),
            Car = team ?? Team(),
            BallastKg = ballastKg,
            SetupQuality = setupQuality,
            ClassIndex = 0,
        };
    }

    public static LapContext Lap(
        Competitor competitor,
        CircuitSpec circuit,
        Coefficients coeff,
        double fuelLaps = 0,
        double wear = 0,
        TyreCompound tyre = TyreCompound.Hard,
        double wetness = 0,
        bool qualifying = false)
    {
        return new LapContext
        {
            Competitor = competitor,
            Circuit = circuit,
            Coefficients = coeff,
            FuelLapsRemaining = fuelLaps,
            TyreWearPercent = wear,
            Tyre = tyre,
            Wetness = wetness,
            QualifyingTrim = qualifying,
        };
    }
}
