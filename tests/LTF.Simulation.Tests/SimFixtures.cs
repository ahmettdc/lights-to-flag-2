using LTF.Domain;
using LTF.Domain.Common;
using LTF.Domain.Racing;

namespace LTF.Simulation.Tests;

internal static class SimFixtures
{
    public static Circuit Circuit(double baseLap = 80.0, int power = 50, int downforce = 50) => new()
    {
        Id = "c",
        Name = "Test Circuit",
        Laps = 50,
        LapDistanceKm = 5.0,
        BaseLapTimeSeconds = baseLap,
        PowerSensitivity = new Rating(power),
        DownforceSensitivity = new Rating(downforce),
    };

    public static Car Car(int flat) => Car(flat, flat);

    public static Car Car(int flat, int reliability) => new()
    {
        Aerodynamics = new Rating(flat),
        Chassis = new Rating(flat),
        PowerUnit = new Rating(flat),
        TyreGentleness = new Rating(flat),
        Reliability = new Rating(reliability),
    };

    public static DriverAttributes Attributes(int flat) => new()
    {
        Pace = new Rating(flat),
        Racecraft = new Rating(flat),
        Consistency = new Rating(flat),
        TyreManagement = new Rating(flat),
        WetWeather = new Rating(flat),
        Feedback = new Rating(flat),
    };

    public static BalanceCoefficients Balance => new();

    /// <summary>Balance with reliability switched off — no failures, no health drain — for
    /// tests that assume every car reaches the flag.</summary>
    public static BalanceCoefficients CalmBalance =>
        new() { ReliabilityFailureRate = 0.0, ComponentHealthLossPerLap = 0.0 };

    public static Carset Carset()
    {
        Driver Make(string id) => new()
        {
            Id = id, FirstName = id.ToUpperInvariant(), LastName = "Driver", Age = 25,
            Attributes = Attributes(70),
        };

        Driver[] drivers = [Make("d1"), Make("d2"), Make("d3"), Make("d4")];

        Team MakeTeam(string id, int flat, string a, string b) => new()
        {
            Id = id, Name = id, Car = Car(flat), DriverIds = [a, b],
        };

        return new Carset
        {
            Id = "t",
            Name = "T",
            Rules = new RulesSet { SeriesName = "S", Points = new PointsScheme { RacePoints = [25, 18] } },
            Teams = [MakeTeam("alpha", 85, "d1", "d2"), MakeTeam("bravo", 70, "d3", "d4")],
            Drivers = drivers,
            Circuits = [Circuit()],
            Calendar = [new CalendarRound { Round = 1, CircuitId = "c", Date = new DateOnly(2025, 3, 16) }],
        };
    }

    /// <summary>Two teams with identical pace but very different reliability, so only
    /// reliability drives who retires.</summary>
    public static Carset ReliabilityContrastCarset()
    {
        Driver Make(string id) => new()
        {
            Id = id, FirstName = id.ToUpperInvariant(), LastName = "Driver", Age = 25,
            Attributes = Attributes(70),
        };

        Driver[] drivers = [Make("h1"), Make("h2"), Make("f1"), Make("f2")];

        Team hardy = new() { Id = "hardy", Name = "hardy", Car = Car(70, 100), DriverIds = ["h1", "h2"] };
        Team fragile = new() { Id = "fragile", Name = "fragile", Car = Car(70, 15), DriverIds = ["f1", "f2"] };

        return new Carset
        {
            Id = "rc",
            Name = "RC",
            Rules = new RulesSet { SeriesName = "S", Points = new PointsScheme { RacePoints = [25, 18] } },
            Teams = [hardy, fragile],
            Drivers = drivers,
            Circuits = [Circuit()],
            Calendar = [new CalendarRound { Round = 1, CircuitId = "c", Date = new DateOnly(2025, 3, 16) }],
        };
    }

    public static Carset CarsetWithUnknownSeat()
    {
        var carset = Carset();
        var brokenTeam = carset.Teams[0] with { DriverIds = ["ghost", "d2"] };
        return carset with { Teams = [brokenTeam, carset.Teams[1]] };
    }
}
