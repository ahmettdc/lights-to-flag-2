using System.Linq;
using LTF.Content;
using Xunit;

namespace LTF.Content.Tests;

public class TechTreeValidatorTests
{
    // A carset with a small, internally consistent tech tree — the baseline each test breaks one way.
    private const string Base =
        """
        {
          "id": "t", "name": "T",
          "rules": { "seriesName": "S", "points": { "racePoints": [25, 18] } },
          "techTree": {
            "departments": [ { "id": "aero", "name": "Aero" } ],
            "nodes": [
              { "id": "floor1", "department": "aero", "category": "AeroFloor", "cost": 5000000, "gainMin": 1, "gainMax": 3 },
              { "id": "floor2", "department": "aero", "category": "AeroFloor", "cost": 8000000, "gainMin": 2, "gainMax": 4, "prerequisites": [ "floor1" ] }
            ]
          },
          "circuits": [ { "id": "c1", "name": "C1", "laps": 50, "lapDistanceKm": 5.0, "baseLapTimeSeconds": 80.0 } ],
          "calendar": [ { "round": 1, "circuitId": "c1", "date": "2025-03-16" } ],
          "teams": [
            { "id": "tm", "name": "Team",
              "car": { "aerodynamics": 80, "chassis": 80, "powerUnit": 80, "tyreGentleness": 80, "reliability": 80 },
              "driverIds": [ "d1", "d2" ],
              "research": { "unlockedNodeIds": [ "floor1" ] } }
          ],
          "drivers": [
            { "id": "d1", "firstName": "A", "lastName": "B", "age": 25,
              "attributes": { "pace": 80, "racecraft": 81, "consistency": 82, "tyreManagement": 83, "wetWeather": 84, "feedback": 85 } },
            { "id": "d2", "firstName": "C", "lastName": "D", "age": 26,
              "attributes": { "pace": 70, "racecraft": 71, "consistency": 72, "tyreManagement": 73, "wetWeather": 74, "feedback": 75 } }
          ]
        }
        """;

    private static bool HasError(string json, string fragment) =>
        CarsetValidator.Validate(CarsetLoader.LoadFromJson(json))
            .Any(i => i.Severity == ValidationSeverity.Error && i.Message.Contains(fragment, StringComparison.Ordinal));

    [Fact]
    public void A_consistent_tech_tree_validates_cleanly()
    {
        var errors = CarsetValidator.Validate(CarsetLoader.LoadFromJson(Base))
            .Count(i => i.Severity == ValidationSeverity.Error);

        Assert.Equal(0, errors);
    }

    [Fact]
    public void A_node_in_an_unknown_department_is_rejected()
    {
        var json = Base.Replace("\"department\": \"aero\"", "\"department\": \"ghost\"", StringComparison.Ordinal);

        Assert.True(HasError(json, "unknown department"));
    }

    [Fact]
    public void A_duplicate_node_id_is_rejected()
    {
        var json = Base.Replace("\"id\": \"floor2\"", "\"id\": \"floor1\"", StringComparison.Ordinal);

        Assert.True(HasError(json, "duplicate tech node id"));
    }

    [Fact]
    public void An_unknown_prerequisite_is_rejected()
    {
        var json = Base.Replace("\"prerequisites\": [ \"floor1\" ]", "\"prerequisites\": [ \"ghost\" ]", StringComparison.Ordinal);

        Assert.True(HasError(json, "requires unknown node"));
    }

    [Fact]
    public void Unlocking_an_unknown_node_is_rejected()
    {
        var json = Base.Replace("\"unlockedNodeIds\": [ \"floor1\" ]", "\"unlockedNodeIds\": [ \"ghost\" ]", StringComparison.Ordinal);

        Assert.True(HasError(json, "unlocked unknown node"));
    }

    [Fact]
    public void A_negative_node_cost_is_rejected()
    {
        var json = Base.Replace("\"cost\": 5000000", "\"cost\": -1", StringComparison.Ordinal);

        Assert.True(HasError(json, "negative cost"));
    }
}
