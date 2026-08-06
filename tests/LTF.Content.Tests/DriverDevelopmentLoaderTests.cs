using LTF.Content;
using Xunit;

namespace LTF.Content.Tests;

public class DriverDevelopmentLoaderTests
{
    private const string WithDevelopment =
        """
        {
          "id": "t", "name": "T",
          "rules": {
            "seriesName": "S",
            "points": { "racePoints": [25, 18, 15] },
            "driverDevelopment": { "peakAgeStart": 27, "peakAgeEnd": 32, "growthPerSeason": 0.25, "physicalDeclinePerSeason": 2.0, "experienceDeclinePerSeason": 0.5, "developmentSpread": 1.5 },
            "regulationUnreadinessPenalty": 0.5
          },
          "circuits": [ { "id": "c1", "name": "C1", "laps": 50, "lapDistanceKm": 5.0, "baseLapTimeSeconds": 80.0 } ],
          "calendar": [ { "round": 1, "circuitId": "c1", "date": "2025-03-16" } ],
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
    public void Loads_the_driver_development_block()
    {
        var rules = CarsetLoader.LoadFromJson(WithDevelopment).Rules;
        var dev = rules.DriverDevelopment;

        Assert.True(dev.IsActive);
        Assert.Equal(27, dev.PeakAgeStart);
        Assert.Equal(32, dev.PeakAgeEnd);
        Assert.Equal(0.25, dev.GrowthPerSeason);
        Assert.Equal(2.0, dev.PhysicalDeclinePerSeason);
        Assert.Equal(0.5, dev.ExperienceDeclinePerSeason);
        Assert.Equal(1.5, dev.DevelopmentSpread);
        Assert.Equal(0.5, rules.RegulationUnreadinessPenalty);
    }

    [Fact]
    public void A_carset_without_a_development_block_is_inert()
    {
        var rules = CarsetLoader.LoadFromJson(TestData.MinimalValid).Rules;

        Assert.False(rules.DriverDevelopment.IsActive);
        Assert.Equal(0.0, rules.RegulationUnreadinessPenalty);
    }

    [Fact]
    public void A_negative_development_coefficient_is_an_error()
    {
        var json = WithDevelopment.Replace(
            "\"growthPerSeason\": 0.25", "\"growthPerSeason\": -0.1", StringComparison.Ordinal);

        var issues = CarsetValidator.Validate(CarsetLoader.LoadFromJson(json));

        Assert.Contains(issues, i => i.Severity == ValidationSeverity.Error
            && i.Message.Contains("driver development coefficients must be non-negative", StringComparison.Ordinal));
    }

    [Fact]
    public void A_negative_regulation_penalty_is_an_error()
    {
        var json = WithDevelopment.Replace(
            "\"regulationUnreadinessPenalty\": 0.5", "\"regulationUnreadinessPenalty\": -1", StringComparison.Ordinal);

        var issues = CarsetValidator.Validate(CarsetLoader.LoadFromJson(json));

        Assert.Contains(issues, i => i.Severity == ValidationSeverity.Error
            && i.Message.Contains("regulationUnreadinessPenalty must be non-negative", StringComparison.Ordinal));
    }
}
