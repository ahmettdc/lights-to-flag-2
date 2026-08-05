using LTF.Content;
using Xunit;

namespace LTF.Content.Tests;

public class EconomyLoaderTests
{
    private const string WithEconomy =
        """
        {
          "id": "t", "name": "T",
          "rules": {
            "seriesName": "S",
            "points": { "racePoints": [25, 18, 15] },
            "economy": { "prizeMoney": [100000000, 60000000, 40000000], "tvIncome": 25000000, "operatingCostPerRace": 2000000, "crashCostPerIncident": 500000 }
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
    public void Loads_the_economy_block()
    {
        var economy = CarsetLoader.LoadFromJson(WithEconomy).Rules.Economy;

        Assert.Equal(100_000_000L, economy.PrizeFor(1));
        Assert.Equal(40_000_000L, economy.PrizeFor(3));
        Assert.Equal(25_000_000L, economy.TvIncome);
        Assert.Equal(2_000_000L, economy.OperatingCostPerRace);
        Assert.Equal(500_000L, economy.CrashCostPerIncident);
    }

    [Fact]
    public void A_carset_without_an_economy_block_is_inert()
    {
        var economy = CarsetLoader.LoadFromJson(TestData.MinimalValid).Rules.Economy;

        Assert.Empty(economy.PrizeMoney);
        Assert.Equal(0L, economy.TvIncome);
    }

    [Fact]
    public void A_negative_economy_value_is_an_error()
    {
        var json = WithEconomy.Replace("\"tvIncome\": 25000000", "\"tvIncome\": -1", StringComparison.Ordinal);

        var issues = CarsetValidator.Validate(CarsetLoader.LoadFromJson(json));

        Assert.Contains(issues, i => i.Severity == ValidationSeverity.Error
            && i.Message.Contains("economy values must be non-negative", StringComparison.Ordinal));
    }
}
