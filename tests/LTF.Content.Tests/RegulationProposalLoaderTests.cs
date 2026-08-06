using LTF.Content;
using LTF.Domain.Rnd;
using Xunit;

namespace LTF.Content.Tests;

public class RegulationProposalLoaderTests
{
    private const string WithProposals =
        """
        {
          "id": "t",
          "name": "T",
          "rules": { "seriesName": "S", "points": { "racePoints": [25, 18, 15] } },
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
              "attributes": { "pace": 80, "racecraft": 81, "consistency": 82, "tyreManagement": 83, "wetWeather": 84, "feedback": 85 } },
            { "id": "d2", "firstName": "C", "lastName": "D", "age": 26,
              "attributes": { "pace": 70, "racecraft": 71, "consistency": 72, "tyreManagement": 73, "wetWeather": 74, "feedback": 75 } }
          ],
          "regulationProposals": [
            { "id": "floor-2027", "description": "Revised floor rules", "favoredAxis": "AeroFloor", "magnitude": 40 },
            { "id": "power-2027", "favoredAxis": "PowerUnit", "magnitude": 30 }
          ]
        }
        """;

    [Fact]
    public void Loads_regulation_proposals()
    {
        var carset = CarsetLoader.LoadFromJson(WithProposals);

        Assert.Equal(2, carset.RegulationProposals.Count);
        var floor = carset.RegulationProposals.Single(p => p.Id == "floor-2027");
        Assert.Equal(CarAxis.AeroFloor, floor.FavoredAxis);
        Assert.Equal(40, floor.Magnitude);
        Assert.Equal("Revised floor rules", floor.Description);

        var power = carset.RegulationProposals.Single(p => p.Id == "power-2027");
        Assert.Equal(CarAxis.PowerUnit, power.FavoredAxis);
        Assert.Equal("", power.Description); // absent description defaults to empty
    }

    [Fact]
    public void A_carset_without_proposals_has_none()
    {
        Assert.Empty(CarsetLoader.LoadFromJson(TestData.MinimalValid).RegulationProposals);
    }

    [Fact]
    public void Valid_proposals_produce_no_validation_issues()
    {
        Assert.Empty(CarsetValidator.Validate(CarsetLoader.LoadFromJson(WithProposals)));
    }

    [Fact]
    public void A_duplicate_proposal_id_is_an_error()
    {
        var json = WithProposals.Replace(
            "\"id\": \"power-2027\"", "\"id\": \"floor-2027\"", StringComparison.Ordinal);

        var issues = CarsetValidator.Validate(CarsetLoader.LoadFromJson(json));

        Assert.Contains(issues, i => i.Severity == ValidationSeverity.Error
            && i.Message.Contains("duplicate regulation proposal id 'floor-2027'", StringComparison.Ordinal));
    }

    [Fact]
    public void An_out_of_range_magnitude_is_an_error()
    {
        var json = WithProposals.Replace("\"magnitude\": 40", "\"magnitude\": 150", StringComparison.Ordinal);

        var issues = CarsetValidator.Validate(CarsetLoader.LoadFromJson(json));

        Assert.Contains(issues, i => i.Severity == ValidationSeverity.Error
            && i.Message.Contains("magnitude must be within 0..100", StringComparison.Ordinal));
    }
}
