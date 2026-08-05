using LTF.Domain;
using LTF.Domain.Common;
using LTF.Domain.Racing;
using LTF.Simulation.Racing;

namespace LTF.Career.Tests;

internal static class CareerFixtures
{
    private static Rating R(int v) => new(v);

    private static DriverAttributes Attributes() => new()
    {
        Pace = R(70), Racecraft = R(70), Consistency = R(70),
        TyreManagement = R(70), WetWeather = R(70), Feedback = R(70),
    };

    private static Driver Driver(string id) => new()
    {
        Id = id, FirstName = id, LastName = "Driver", Age = 25, Attributes = Attributes(),
    };

    private static Car Car() => new()
    {
        Aerodynamics = R(70), Chassis = R(70), PowerUnit = R(70), TyreGentleness = R(70), Reliability = R(70),
    };

    /// <summary>A minimal two-team, four-driver carset: alpha (d1/d2) and bravo (d3/d4).</summary>
    public static Carset Carset() => new()
    {
        Id = "cr",
        Name = "Career Test",
        Rules = new RulesSet { SeriesName = "S", Points = new PointsScheme { RacePoints = [25, 18, 15, 12] } },
        Teams =
        [
            new Team { Id = "alpha", Name = "Alpha", Car = Car(), DriverIds = ["d1", "d2"] },
            new Team { Id = "bravo", Name = "Bravo", Car = Car(), DriverIds = ["d3", "d4"] },
        ],
        Drivers = [Driver("d1"), Driver("d2"), Driver("d3"), Driver("d4")],
        Circuits = [new Circuit { Id = "c", Name = "C", Laps = 50, LapDistanceKm = 5.0, BaseLapTimeSeconds = 80.0 }],
        Calendar = [new CalendarRound { Round = 1, CircuitId = "c", Date = new DateOnly(2025, 3, 16) }],
    };

    /// <summary>A race result with the given finishers in order (positions 1..n, all finished),
    /// each carrying the supplied championship points.</summary>
    public static RaceResult Race(params (string Id, int Points)[] finishers)
    {
        var entries = finishers
            .Select((f, i) => new RaceClassificationEntry
            {
                Position = i + 1,
                CompetitorId = f.Id,
                Status = FinishStatus.Finished,
                Laps = 50,
                TotalTime = 5000.0 + i,
                GapToLeader = i,
                BestLap = 80.0,
                TopSpeed = 300.0,
                Points = f.Points,
            })
            .ToList();

        return new RaceResult
        {
            Classification = entries,
            Telemetry = new RaceTelemetry { Laps = [] },
            Events = [],
        };
    }
}
