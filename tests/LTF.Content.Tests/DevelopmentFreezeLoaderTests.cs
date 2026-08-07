using System.Linq;
using LTF.Content;
using LTF.Domain.Rnd;
using Xunit;

namespace LTF.Content.Tests;

public class DevelopmentFreezeLoaderTests
{
    private const string WithFreezes =
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
          "regulations": {
            "developmentFreezes": [
              { "axis": "PowerUnit", "mode": "InSeasonOnly" },
              { "axis": "MechanicalGrip", "mode": "Full" }
            ]
          }
        }
        """;

    [Fact]
    public void Loads_development_freezes()
    {
        var carset = CarsetLoader.LoadFromJson(WithFreezes);

        Assert.Equal(2, carset.Regulations.DevelopmentFreezes.Count);
        var pu = carset.Regulations.DevelopmentFreezes.Single(f => f.Axis == CarAxis.PowerUnit);
        Assert.Equal(DevelopmentFreezeMode.InSeasonOnly, pu.Mode);
        var mech = carset.Regulations.DevelopmentFreezes.Single(f => f.Axis == CarAxis.MechanicalGrip);
        Assert.Equal(DevelopmentFreezeMode.Full, mech.Mode);
    }

    [Fact]
    public void A_carset_without_a_regulations_block_has_no_freezes()
    {
        Assert.Empty(CarsetLoader.LoadFromJson(TestData.MinimalValid).Regulations.DevelopmentFreezes);
    }

    [Fact]
    public void Valid_freezes_produce_no_validation_issues()
    {
        Assert.Empty(CarsetValidator.Validate(CarsetLoader.LoadFromJson(WithFreezes)));
    }
}
