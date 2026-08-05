using System.Linq;
using LTF.Content;
using LTF.Domain.Rnd;
using Xunit;

namespace LTF.Content.Tests;

public class TechTreeLoaderTests
{
    private const string WithRnd =
        """
        {
          "id": "t", "name": "T",
          "rules": {
            "seriesName": "S",
            "points": { "racePoints": [25, 18] },
            "research": { "baseProgressPerSeason": 40, "facilityWeight": 0.5, "maxRetries": 2 }
          },
          "techTree": {
            "departments": [ { "id": "aero", "name": "Aerodynamics" } ],
            "nodes": [
              { "id": "floor1", "department": "aero", "category": "AeroFloor", "size": "Major",
                "cost": 5000000, "quota": 10, "gainMin": 1, "gainMax": 3, "confidence": 80, "correlation": 90 }
            ]
          },
          "staffPool": [
            { "id": "fa1", "firstName": "Free", "lastName": "Agent", "role": "ChiefAerodynamicist", "skill": 84, "salary": 3000000 }
          ],
          "circuits": [ { "id": "c1", "name": "C1", "laps": 50, "lapDistanceKm": 5.0, "baseLapTimeSeconds": 80.0 } ],
          "calendar": [ { "round": 1, "circuitId": "c1", "date": "2025-03-16" } ],
          "teams": [
            { "id": "tm", "name": "Team",
              "car": { "aerodynamics": 80, "chassis": 80, "powerUnit": 80, "tyreGentleness": 80, "reliability": 80 },
              "driverIds": [ "d1", "d2" ],
              "research": { "unlockedNodeIds": [ "floor1" ], "regulationReadiness": 25,
                            "activeProjects": [ { "nodeId": "floor1", "state": "InManufacture" } ] } }
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
    public void Loads_the_tech_tree_catalog()
    {
        var carset = CarsetLoader.LoadFromJson(WithRnd);

        Assert.Equal("aero", carset.TechTree.Departments.Single().Id);

        var node = carset.TechTree.Nodes.Single();
        Assert.Equal("floor1", node.Id);
        Assert.Equal("aero", node.DepartmentId);
        Assert.Equal(CarAxis.AeroFloor, node.Category);
        Assert.Equal(NodeSize.Major, node.Size);
        Assert.Equal(5_000_000L, node.Cost);
        Assert.Equal(3, node.GainMax);
    }

    [Fact]
    public void Loads_a_teams_research_progress()
    {
        var team = CarsetLoader.LoadFromJson(WithRnd).Teams.Single();

        Assert.Equal("floor1", team.Research.UnlockedNodeIds.Single());
        Assert.Equal(25, team.Research.RegulationReadiness);

        var project = team.Research.ActiveProjects.Single();
        Assert.Equal("floor1", project.NodeId);
        Assert.Equal(ValidationState.InManufacture, project.State);
    }

    [Fact]
    public void Loads_the_staff_pool_and_research_rules()
    {
        var carset = CarsetLoader.LoadFromJson(WithRnd);

        Assert.Equal("fa1", carset.StaffPool.Single().Id);
        Assert.Equal(40, carset.Rules.Research.BaseProgressPerSeason);
        Assert.Equal(0.5, carset.Rules.Research.FacilityWeight);
        Assert.Equal(2, carset.Rules.Research.MaxRetries);
    }

    [Fact]
    public void A_carset_without_a_tech_tree_is_inert()
    {
        var carset = CarsetLoader.LoadFromJson(TestData.MinimalValid);

        Assert.Empty(carset.TechTree.Nodes);
        Assert.Empty(carset.StaffPool);
        Assert.Equal(ResearchState.Empty, carset.Teams.Single().Research);
        Assert.Equal(0, carset.Rules.Research.BaseProgressPerSeason);
    }
}
