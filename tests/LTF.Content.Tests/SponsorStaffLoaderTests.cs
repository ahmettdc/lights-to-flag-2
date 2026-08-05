using System.Linq;
using LTF.Content;
using LTF.Domain.Common;
using Xunit;

namespace LTF.Content.Tests;

public class SponsorStaffLoaderTests
{
    private const string WithSponsorsAndStaff =
        """
        {
          "id": "t", "name": "T",
          "rules": { "seriesName": "S", "points": { "racePoints": [25, 18, 15] } },
          "circuits": [ { "id": "c1", "name": "C1", "laps": 50, "lapDistanceKm": 5.0, "baseLapTimeSeconds": 80.0 } ],
          "calendar": [ { "round": 1, "circuitId": "c1", "date": "2025-03-16" } ],
          "teams": [
            {
              "id": "tm", "name": "Team",
              "car": { "aerodynamics": 80, "chassis": 80, "powerUnit": 80, "tyreGentleness": 80, "reliability": 80 },
              "driverIds": [ "d1", "d2" ],
              "finances": { "balance": 120000000, "costCap": 135000000 },
              "sponsors": [
                { "id": "titan", "name": "Titan Systems", "tier": "Title", "perRaceFee": 3000000, "perPointBonus": 50000, "objectiveBonus": 10000000, "objectivePosition": 3 },
                { "id": "orbit", "name": "Orbit Drinks", "tier": "Secondary", "perRaceFee": 800000 }
              ],
              "staff": [
                { "id": "td", "firstName": "Tec", "lastName": "Dir", "role": "TechnicalDirector", "skill": 82, "salary": 4000000 },
                { "id": "re", "firstName": "Race", "lastName": "Eng", "role": "RaceEngineer", "skill": 74, "salary": 1500000 }
              ]
            }
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
    public void Loads_a_teams_sponsors()
    {
        var team = CarsetLoader.LoadFromJson(WithSponsorsAndStaff).Teams.Single();

        Assert.Equal(2, team.Sponsors.Count);

        var titan = team.Sponsors.Single(s => s.Id == "titan");
        Assert.Equal(SponsorTier.Title, titan.Tier);
        Assert.Equal(3_000_000L, titan.PerRaceFee);
        Assert.Equal(50_000L, titan.PerPointBonus);
        Assert.Equal(10_000_000L, titan.ObjectiveBonus);
        Assert.Equal(3, titan.ObjectivePosition);

        var orbit = team.Sponsors.Single(s => s.Id == "orbit");
        Assert.Equal(SponsorTier.Secondary, orbit.Tier);
        Assert.Equal(0L, orbit.PerPointBonus); // absent → 0
    }

    [Fact]
    public void Loads_a_teams_staff()
    {
        var team = CarsetLoader.LoadFromJson(WithSponsorsAndStaff).Teams.Single();

        Assert.Equal(2, team.Staff.Count);

        var td = team.Staff.Single(m => m.Id == "td");
        Assert.Equal(StaffRole.TechnicalDirector, td.Role);
        Assert.Equal(82, td.Skill.Value);
        Assert.Equal(4_000_000L, td.Salary);
    }

    [Fact]
    public void A_team_without_sponsors_or_staff_is_empty()
    {
        var team = CarsetLoader.LoadFromJson(TestData.MinimalValid).Teams.Single();

        Assert.Empty(team.Sponsors);
        Assert.Empty(team.Staff);
    }

    [Fact]
    public void An_unknown_staff_role_is_rejected()
    {
        var json = WithSponsorsAndStaff.Replace("\"role\": \"TechnicalDirector\"", "\"role\": \"Chef\"", StringComparison.Ordinal);

        var ex = Assert.Throws<CarsetValidationException>(() => CarsetLoader.LoadFromJson(json));
        Assert.Contains("role", ex.Message);
    }

    [Fact]
    public void A_negative_salary_is_a_validation_error()
    {
        var json = WithSponsorsAndStaff.Replace("\"salary\": 4000000", "\"salary\": -1", StringComparison.Ordinal);

        var issues = CarsetValidator.Validate(CarsetLoader.LoadFromJson(json));

        Assert.Contains(issues, i => i.Severity == ValidationSeverity.Error
            && i.Message.Contains("negative salary", StringComparison.Ordinal));
    }
}
