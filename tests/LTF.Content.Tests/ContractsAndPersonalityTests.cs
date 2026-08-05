using LTF.Content;
using LTF.Domain.Common;
using LTF.Domain.Racing;
using Xunit;

namespace LTF.Content.Tests;

public class ContractsAndPersonalityTests
{
    private const string WithContractsAndPersonality =
        """
        {
          "id": "t",
          "name": "T",
          "rules": { "seriesName": "S", "points": { "racePoints": [25, 18, 15] } },
          "tyres": [ { "compound": "Soft", "code": "C4", "grip": 90, "durability": 40 } ],
          "circuits": [
            { "id": "c1", "name": "C1", "laps": 50, "lapDistanceKm": 5.0, "baseLapTimeSeconds": 80.0 }
          ],
          "calendar": [ { "round": 1, "circuitId": "c1", "date": "2025-03-16" } ],
          "teams": [
            {
              "id": "tm", "name": "Team",
              "car": { "aerodynamics": 80, "chassis": 80, "powerUnit": 80, "tyreGentleness": 80, "reliability": 80 },
              "driverIds": [ "d1", "d2" ]
            }
          ],
          "drivers": [
            { "id": "d1", "firstName": "A", "lastName": "B", "age": 25,
              "attributes": { "pace": 80, "racecraft": 81, "consistency": 82, "tyreManagement": 83, "wetWeather": 84, "feedback": 85 },
              "personality": { "ego": 80, "loyalty": 40, "temperament": 60, "ambition": 90 } },
            { "id": "d2", "firstName": "C", "lastName": "D", "age": 26,
              "attributes": { "pace": 70, "racecraft": 71, "consistency": 72, "tyreManagement": 73, "wetWeather": 74, "feedback": 75 } }
          ],
          "contracts": [
            { "kind": "Driver", "partyId": "d1", "teamId": "tm", "salaryPerSeason": 5000000, "seasonsRemaining": 2, "signingBonus": 1000000,
              "clauses": { "perPointBonus": 50000, "championshipBonus": 2000000, "exitClause": 10000000, "firstDriverStatus": true } },
            { "kind": "Driver", "partyId": "d2", "teamId": "tm", "salaryPerSeason": 3000000, "seasonsRemaining": 1 }
          ]
        }
        """;

    [Fact]
    public void Loads_contracts_with_their_clauses()
    {
        var carset = CarsetLoader.LoadFromJson(WithContractsAndPersonality);

        Assert.Equal(2, carset.Contracts.Count);

        var d1 = carset.Contracts.Single(c => c.PartyId == "d1");
        Assert.Equal(ContractKind.Driver, d1.Kind);
        Assert.Equal("tm", d1.TeamId);
        Assert.Equal(5_000_000L, d1.SalaryPerSeason);
        Assert.Equal(2, d1.SeasonsRemaining);
        Assert.Equal(1_000_000L, d1.SigningBonus);
        Assert.Equal(50_000L, d1.Clauses.PerPointBonus);
        Assert.True(d1.Clauses.FirstDriverStatus);

        var d2 = carset.Contracts.Single(c => c.PartyId == "d2");
        Assert.Equal(1, d2.SeasonsRemaining);
        Assert.False(d2.Clauses.FirstDriverStatus); // absent clauses default to none
    }

    [Fact]
    public void Loads_a_drivers_personality_and_defaults_the_rest_to_neutral()
    {
        var carset = CarsetLoader.LoadFromJson(WithContractsAndPersonality);

        var d1 = carset.Drivers.Single(d => d.Id == "d1");
        Assert.Equal(80, d1.Personality.Ego.Value);
        Assert.Equal(40, d1.Personality.Loyalty.Value);

        var d2 = carset.Drivers.Single(d => d.Id == "d2");
        Assert.Equal(Personality.Neutral, d2.Personality); // no personality block → neutral
    }

    [Fact]
    public void A_carset_without_contracts_has_none()
    {
        var carset = CarsetLoader.LoadFromJson(TestData.MinimalValid);

        Assert.Empty(carset.Contracts);
    }

    [Fact]
    public void Valid_contracts_produce_no_validation_issues()
    {
        var carset = CarsetLoader.LoadFromJson(WithContractsAndPersonality);

        Assert.Empty(CarsetValidator.Validate(carset));
    }

    [Fact]
    public void A_contract_referencing_an_unknown_team_is_an_error()
    {
        var json = WithContractsAndPersonality.Replace(
            "\"teamId\": \"tm\"", "\"teamId\": \"nope\"", StringComparison.Ordinal);

        var issues = CarsetValidator.Validate(CarsetLoader.LoadFromJson(json));

        Assert.Contains(issues, i => i.Severity == ValidationSeverity.Error
            && i.Message.Contains("unknown team 'nope'", StringComparison.Ordinal));
    }

    [Fact]
    public void A_contract_referencing_an_unknown_driver_is_an_error()
    {
        var json = WithContractsAndPersonality.Replace(
            "\"partyId\": \"d1\"", "\"partyId\": \"dX\"", StringComparison.Ordinal);

        var issues = CarsetValidator.Validate(CarsetLoader.LoadFromJson(json));

        Assert.Contains(issues, i => i.Severity == ValidationSeverity.Error
            && i.Message.Contains("unknown driver 'dX'", StringComparison.Ordinal));
    }

    [Fact]
    public void A_negative_salary_is_an_error()
    {
        var json = WithContractsAndPersonality.Replace(
            "\"salaryPerSeason\": 5000000", "\"salaryPerSeason\": -1", StringComparison.Ordinal);

        var issues = CarsetValidator.Validate(CarsetLoader.LoadFromJson(json));

        Assert.Contains(issues, i => i.Severity == ValidationSeverity.Error
            && i.Message.Contains("negative salary", StringComparison.Ordinal));
    }
}
