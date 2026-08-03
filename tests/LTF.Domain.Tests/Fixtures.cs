using LTF.Domain;
using LTF.Domain.Common;
using LTF.Domain.Racing;

namespace LTF.Domain.Tests;

/// <summary>Small builders for constructing valid domain values in tests.</summary>
internal static class Fixtures
{
    public static DriverAttributes Attributes(int flat = 60) => new()
    {
        Pace = new Rating(flat),
        Racecraft = new Rating(flat),
        Consistency = new Rating(flat),
        TyreManagement = new Rating(flat),
        WetWeather = new Rating(flat),
        Feedback = new Rating(flat),
    };

    public static Driver Driver(string id, string first, string last, int flat = 60) => new()
    {
        Id = id,
        FirstName = first,
        LastName = last,
        Age = 26,
        Attributes = Attributes(flat),
    };

    public static Car Car(int flat = 70) => new()
    {
        Aerodynamics = new Rating(flat),
        Chassis = new Rating(flat),
        PowerUnit = new Rating(flat),
        TyreGentleness = new Rating(flat),
        Reliability = new Rating(flat),
    };

    public static Team Team(string id, string name, params string[] driverIds) => new()
    {
        Id = id,
        Name = name,
        Car = Car(),
        DriverIds = driverIds,
    };

    public static RulesSet Rules() => new()
    {
        SeriesName = "Test Series",
        Points = new PointsScheme { RacePoints = [25, 18, 15, 12, 10, 8, 6, 4, 2, 1] },
    };

    public static Circuit Circuit(string id, string name) => new()
    {
        Id = id,
        Name = name,
        Laps = 50,
        LapDistanceKm = 5.0,
        BaseLapTimeSeconds = 80.0,
    };

    /// <summary>A minimal but fully wired two-team carset.</summary>
    public static Carset Carset()
    {
        var d1 = Driver("ferreira_mateo", "Mateo", "Ferreira");
        var d2 = Driver("whitlock_idris", "Idris", "Whitlock");
        return new Carset
        {
            Id = "test",
            Name = "Test Championship",
            Rules = Rules(),
            Teams = [Team("talon", "Talon Racing", d1.Id, d2.Id)],
            Drivers = [d1, d2],
            Circuits = [Circuit("belgian", "Belgian Grand Prix")],
        };
    }
}
