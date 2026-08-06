using System;
using System.Linq;
using LTF.Content;
using Xunit;

namespace LTF.Content.Tests;

public class TestDayLoaderTests
{
    private const string WithTestDays =
        """
        {
          "id": "t", "name": "T",
          "rules": { "seriesName": "S", "points": { "racePoints": [25, 18] } },
          "circuits": [ { "id": "c1", "name": "C1", "laps": 50, "lapDistanceKm": 5.0, "baseLapTimeSeconds": 80.0 } ],
          "calendar": [ { "round": 1, "circuitId": "c1", "date": "2025-03-16" } ],
          "testDays": [ { "date": "2025-02-20", "circuitId": "c1" } ],
          "teams": [
            { "id": "tm", "name": "Team",
              "car": { "aerodynamics": 80, "chassis": 80, "powerUnit": 80, "tyreGentleness": 80, "reliability": 80 },
              "driverIds": [ "d1", "d2" ] }
          ],
          "drivers": [
            { "id": "d1", "firstName": "A", "lastName": "B", "age": 25,
              "attributes": { "pace": 80, "racecraft": 81, "consistency": 82, "tyreManagement": 83, "wetWeather": 84, "feedback": 85 } },
            { "id": "d2", "firstName": "C", "lastName": "D", "age": 26,
              "attributes": { "pace": 70, "racecraft": 71, "consistency": 72, "tyreManagement": 73, "wetWeather": 74, "feedback": 75 } }
          ]
        }
        """;

    [Fact]
    public void Loads_test_days_and_validates_cleanly()
    {
        var carset = CarsetLoader.LoadFromJson(WithTestDays);

        var test = Assert.Single(carset.TestDays);
        Assert.Equal("c1", test.CircuitId);
        Assert.Equal(new DateOnly(2025, 2, 20), test.Date);
        Assert.Empty(CarsetValidator.Validate(carset));
    }

    [Fact]
    public void A_test_day_at_an_unknown_circuit_is_rejected()
    {
        var json = WithTestDays.Replace("\"circuitId\": \"c1\" }", "\"circuitId\": \"ghost\" }");
        var carset = CarsetLoader.LoadFromJson(json);

        Assert.Contains(CarsetValidator.Validate(carset), i =>
            i.Severity == ValidationSeverity.Error &&
            i.Message.Contains("unknown circuit 'ghost'", StringComparison.Ordinal));
    }

    [Fact]
    public void A_carset_without_test_days_has_none()
    {
        var json = WithTestDays.Replace("\"testDays\": [ { \"date\": \"2025-02-20\", \"circuitId\": \"c1\" } ],", "");
        var carset = CarsetLoader.LoadFromJson(json);

        Assert.Empty(carset.TestDays);
    }
}
