using System.Linq;
using LTF.Content;
using LTF.Domain.Management;
using Xunit;

namespace LTF.Content.Tests;

public class FacilitiesLoaderTests
{
    private const string WithFacilities =
        """
        {
          "id": "t", "name": "T",
          "rules": { "seriesName": "S", "points": { "racePoints": [25, 18] } },
          "circuits": [ { "id": "c1", "name": "C1", "laps": 50, "lapDistanceKm": 5.0, "baseLapTimeSeconds": 80.0 } ],
          "calendar": [ { "round": 1, "circuitId": "c1", "date": "2025-03-16" } ],
          "teams": [
            { "id": "tm", "name": "Team",
              "car": { "aerodynamics": 80, "chassis": 80, "powerUnit": 80, "tyreGentleness": 80, "reliability": 80 },
              "driverIds": [ "d1", "d2" ],
              "facilities": { "designOffice": 4, "windTunnel": 5, "cfd": 2, "dataCentre": 1 } }
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
    public void Loads_a_teams_facility_levels()
    {
        var facilities = CarsetLoader.LoadFromJson(WithFacilities).Teams.Single().Facilities;

        Assert.Equal(4, facilities.DesignOffice.Value);
        Assert.Equal(5, facilities.WindTunnel.Value);
        Assert.Equal(2, facilities.Cfd.Value);
        Assert.Equal(1, facilities.DataCentre.Value);
        Assert.Equal(3, facilities.Simulator.Value); // absent field → default level 3
    }

    [Fact]
    public void A_team_without_facilities_gets_the_default_set()
    {
        var facilities = CarsetLoader.LoadFromJson(TestData.MinimalValid).Teams.Single().Facilities;

        Assert.Equal(Facilities.Default, facilities);
        Assert.Equal(3, facilities.WindTunnel.Value);
    }
}
