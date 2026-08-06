using LTF.Content;
using LTF.Domain.Common;
using LTF.Domain.Management;
using Xunit;

namespace LTF.Content.Tests;

public class BoardLoaderTests
{
    private const string WithBoards =
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
          "playerTeamId": "tm",
          "boards": [
            {
              "teamId": "tm",
              "ownership": "ManufacturerBacked",
              "firingRisk": 20,
              "pressure": {
                "boardConfidence": 60, "sportingPressure": 40, "financialPressure": 30,
                "sponsorPressure": 35, "mediaPressure": 25, "internalPressure": 20
              },
              "members": [
                { "id": "chair", "name": "The Chair", "sportingPriority": 80, "financialPriority": 30,
                  "longTermPriority": 60, "brandPriority": 40, "driverDevPriority": 50,
                  "confidenceInPlayer": 65, "riskTolerance": 55, "traits": [ "Impatient", "Ambitious" ] },
                { "id": "cfo", "financialPriority": 90 }
              ],
              "objectives": [
                { "kind": "ConstructorPosition", "visibility": "Open", "target": 4, "linkedBudget": 5000000, "linkedRisk": 30 },
                { "kind": "FinancialResult", "target": 0 }
              ]
            }
          ]
        }
        """;

    private const string WithMinimalBoard =
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
          "boards": [ { "teamId": "tm" } ]
        }
        """;

    [Fact]
    public void Loads_a_board_with_ownership_members_pressure_and_objectives()
    {
        var carset = CarsetLoader.LoadFromJson(WithBoards);

        Assert.Equal("tm", carset.PlayerTeamId);
        var board = Assert.Single(carset.Boards);
        Assert.Equal("tm", board.TeamId);
        Assert.Equal(OwnershipType.ManufacturerBacked, board.Ownership);
        Assert.Equal(20, board.FiringRisk.Value);

        Assert.Equal(60, board.Metrics.BoardConfidence.Value);
        Assert.Equal(40, board.Metrics.SportingPressure.Value);
        Assert.Equal(20, board.Metrics.InternalPressure.Value);

        var chair = board.Members.Single(m => m.Id == "chair");
        Assert.Equal(80, chair.SportingPriority.Value);
        Assert.Equal(65, chair.ConfidenceInPlayer.Value);
        Assert.Equal(BoardMemberTraits.Impatient | BoardMemberTraits.Ambitious, chair.Traits);

        var cfo = board.Members.Single(m => m.Id == "cfo");
        Assert.Equal(90, cfo.FinancialPriority.Value);
        Assert.Equal(50, cfo.SportingPriority.Value);   // absent priority defaults to 50
        Assert.Equal(50, cfo.ConfidenceInPlayer.Value); // absent confidence defaults to 50
        Assert.Equal(BoardMemberTraits.None, cfo.Traits);

        Assert.Equal(2, board.Objectives.Count);
        var position = board.Objectives[0];
        Assert.Equal(ObjectiveKind.ConstructorPosition, position.Kind);
        Assert.Equal(ObjectiveVisibility.Open, position.Visibility);
        Assert.Equal(4, position.Target);
        Assert.Equal(5_000_000L, position.LinkedBudget);
        Assert.Equal(30, position.LinkedRisk);
        Assert.Equal(ObjectiveVisibility.Open, board.Objectives[1].Visibility); // absent visibility defaults to open
    }

    [Fact]
    public void A_minimal_board_defaults_ownership_pressure_and_lists()
    {
        var carset = CarsetLoader.LoadFromJson(WithMinimalBoard);

        var board = Assert.Single(carset.Boards);
        Assert.Equal(OwnershipType.RacingOwner, board.Ownership);
        Assert.Equal(PressureMetrics.Neutral, board.Metrics);
        Assert.Empty(board.Members);
        Assert.Empty(board.Objectives);
        Assert.Equal(0, board.FiringRisk.Value);
        Assert.Equal("", carset.PlayerTeamId); // a board block does not imply a player team
    }

    [Fact]
    public void A_carset_without_boards_has_no_boards_or_player_team()
    {
        var carset = CarsetLoader.LoadFromJson(TestData.MinimalValid);

        Assert.Empty(carset.Boards);
        Assert.Equal("", carset.PlayerTeamId);
    }

    [Fact]
    public void Valid_boards_produce_no_validation_issues()
    {
        var carset = CarsetLoader.LoadFromJson(WithBoards);

        Assert.Empty(CarsetValidator.Validate(carset));
    }

    [Fact]
    public void An_unknown_player_team_is_an_error()
    {
        var json = WithBoards.Replace(
            "\"playerTeamId\": \"tm\"", "\"playerTeamId\": \"nope\"", StringComparison.Ordinal);

        var issues = CarsetValidator.Validate(CarsetLoader.LoadFromJson(json));

        Assert.Contains(issues, i => i.Severity == ValidationSeverity.Error
            && i.Message.Contains("playerTeamId references unknown team 'nope'", StringComparison.Ordinal));
    }

    [Fact]
    public void A_board_referencing_an_unknown_team_is_an_error()
    {
        var json = WithBoards.Replace(
            "\"teamId\": \"tm\"", "\"teamId\": \"nope\"", StringComparison.Ordinal);

        var issues = CarsetValidator.Validate(CarsetLoader.LoadFromJson(json));

        Assert.Contains(issues, i => i.Severity == ValidationSeverity.Error
            && i.Message.Contains("board references unknown team 'nope'", StringComparison.Ordinal));
    }

    [Fact]
    public void A_negative_linked_budget_is_an_error()
    {
        var json = WithBoards.Replace(
            "\"linkedBudget\": 5000000", "\"linkedBudget\": -1", StringComparison.Ordinal);

        var issues = CarsetValidator.Validate(CarsetLoader.LoadFromJson(json));

        Assert.Contains(issues, i => i.Severity == ValidationSeverity.Error
            && i.Message.Contains("negative linked budget", StringComparison.Ordinal));
    }
}
