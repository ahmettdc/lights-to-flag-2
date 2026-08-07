using LTF.Content;
using Xunit;

namespace LTF.Content.Tests;

public class BankLoaderTests
{
    private const string WithBank =
        """
        {
          "id": "t", "name": "T",
          "rules": {
            "seriesName": "S",
            "points": { "racePoints": [25, 18, 15] },
            "bank": { "baseRatePercent": 6, "maxRiskPremiumPercent": 24, "maxLoanToRevenuePercent": 200, "latePenaltyPercent": 10, "assetSeizureAfterMisses": 2, "insolvencyAfterMisses": 4, "insolvencyPointsPenalty": 15 }
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
    public void Loads_the_bank_block()
    {
        var bank = CarsetLoader.LoadFromJson(WithBank).Rules.Bank;

        Assert.True(bank.IsActive);
        Assert.Equal(6, bank.BaseRatePercent);
        Assert.Equal(24, bank.MaxRiskPremiumPercent);
        Assert.Equal(200, bank.MaxLoanToRevenuePercent);
        Assert.Equal(10, bank.LatePenaltyPercent);
        Assert.Equal(2, bank.AssetSeizureAfterMisses);
        Assert.Equal(4, bank.InsolvencyAfterMisses);
        Assert.Equal(15, bank.InsolvencyPointsPenalty);
    }

    [Fact]
    public void A_carset_without_a_bank_block_is_inert()
    {
        var bank = CarsetLoader.LoadFromJson(TestData.MinimalValid).Rules.Bank;

        Assert.False(bank.IsActive);
        Assert.Equal(0, bank.MaxLoanToRevenuePercent);
        Assert.Equal(0, bank.BaseRatePercent);
    }

    [Fact]
    public void A_negative_bank_value_is_an_error()
    {
        var json = WithBank.Replace("\"baseRatePercent\": 6", "\"baseRatePercent\": -1", StringComparison.Ordinal);

        var issues = CarsetValidator.Validate(CarsetLoader.LoadFromJson(json));

        Assert.Contains(issues, i => i.Severity == ValidationSeverity.Error
            && i.Message.Contains("bank values must be non-negative", StringComparison.Ordinal));
    }
}
