using System.Linq;
using LTF.Domain;
using LTF.Domain.Common;
using LTF.Domain.Management;
using Xunit;

namespace LTF.Career.Tests;

public class BoardReviewTests
{
    // A one-board carset (alpha the player) with a sporting-minded chair and a finance-minded CFO.
    private static Carset BoardCarset(params Objective[] objectives)
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 4) with { PlayerTeamId = "alpha" };
        var board = new TeamBoard
        {
            TeamId = "alpha",
            Members = [Member("chair", sporting: 80, financial: 20), Member("cfo", sporting: 20, financial: 80)],
            Objectives = objectives,
        };
        return carset with { Boards = [board] };
    }

    private static Standings StandingsWith(int alphaPosition, int alphaPoints) => new()
    {
        Drivers = [],
        Constructors =
        [
            new ConstructorStanding { Position = alphaPosition, TeamId = "alpha", Points = alphaPoints, Wins = 0 },
            new ConstructorStanding { Position = alphaPosition == 1 ? 2 : 1, TeamId = "bravo", Points = 0, Wins = 0 },
        ],
    };

    [Fact]
    public void Meeting_the_objective_lifts_board_confidence()
    {
        var carset = BoardCarset(new Objective { Kind = ObjectiveKind.ConstructorPosition, Target = 3 });
        var board = BoardReview.Assess(carset, StandingsWith(alphaPosition: 1, alphaPoints: 100), seed: 7).Boards.Single();

        Assert.True(board.Metrics.BoardConfidence.Value > 50); // rose from the neutral 50
    }

    [Fact]
    public void Missing_the_objective_erodes_confidence_and_raises_firing_risk()
    {
        var carset = BoardCarset(new Objective { Kind = ObjectiveKind.ConstructorPosition, Target = 3 });
        var board = BoardReview.Assess(carset, StandingsWith(alphaPosition: 8, alphaPoints: 5), seed: 7).Boards.Single();

        Assert.True(board.Metrics.BoardConfidence.Value < 50);  // confidence fell
        Assert.True(board.Metrics.SportingPressure.Value > 50); // sporting pressure rose
        Assert.True(board.FiringRisk.Value > 0);                // the sack looms
    }

    [Fact]
    public void Members_react_to_the_category_they_weight_most()
    {
        // A sporting objective is missed; there is no financial objective. The sporting-minded chair loses
        // confidence, the finance-minded CFO (reacting to the neutral financial category) does not.
        var carset = BoardCarset(new Objective { Kind = ObjectiveKind.ConstructorPosition, Target = 3 });
        var board = BoardReview.Assess(carset, StandingsWith(alphaPosition: 10, alphaPoints: 0), seed: 7).Boards.Single();

        Assert.True(board.Members.Single(m => m.Id == "chair").ConfidenceInPlayer.Value < 50);
        Assert.True(board.Members.Single(m => m.Id == "cfo").ConfidenceInPlayer.Value >= 50);
    }

    [Fact]
    public void A_carset_without_boards_is_untouched_by_assessment()
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 4);

        Assert.Same(carset, BoardReview.Assess(carset, StandingsWith(1, 100), 7));
    }

    [Fact]
    public void Assessment_is_deterministic()
    {
        var carset = BoardCarset(new Objective { Kind = ObjectiveKind.ConstructorPosition, Target = 3 });
        var standings = StandingsWith(8, 5);

        Assert.Equal(
            Key(BoardReview.Assess(carset, standings, 7)),
            Key(BoardReview.Assess(carset, standings, 7)));
    }

    [Fact]
    public void Set_objectives_fills_a_boardless_board_from_its_ownership()
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 4) with
        {
            PlayerTeamId = "alpha",
            Boards = [new TeamBoard { TeamId = "alpha", Ownership = OwnershipType.FinanceBoard }],
        };

        var board = BoardReview.SetObjectives(carset).Boards.Single();

        Assert.Equal(ObjectiveKind.FinancialResult, Assert.Single(board.Objectives).Kind);
    }

    [Fact]
    public void Set_objectives_keeps_content_authored_objectives()
    {
        var carset = BoardCarset(new Objective { Kind = ObjectiveKind.RaceWins, Target = 5 });

        var board = BoardReview.SetObjectives(carset).Boards.Single();

        Assert.Equal(ObjectiveKind.RaceWins, Assert.Single(board.Objectives).Kind);
    }

    [Fact]
    public void A_board_carset_gets_board_review_calendar_events()
    {
        var carset = BoardCarset(new Objective { Kind = ObjectiveKind.ConstructorPosition, Target = 3 });

        Assert.Contains(SeasonCalendar.ForCareer(carset).Events, e => e.Kind == CalendarEventKind.BoardReview);
    }

    [Fact]
    public void A_boardless_carset_gets_no_board_review_calendar_events()
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 4);

        Assert.DoesNotContain(SeasonCalendar.ForCareer(carset).Events, e => e.Kind == CalendarEventKind.BoardReview);
    }

    private static BoardMember Member(string id, int sporting, int financial) => new()
    {
        Id = id,
        SportingPriority = new(sporting),
        FinancialPriority = new(financial),
        LongTermPriority = new(50),
        BrandPriority = new(50),
        DriverDevPriority = new(50),
        RiskTolerance = new(50),
    };

    private static string Key(Carset carset) =>
        string.Join(";", carset.Boards.Select(b =>
            $"{b.TeamId}:{b.Metrics.BoardConfidence.Value},{b.Metrics.SportingPressure.Value},{b.FiringRisk.Value}," +
            string.Join(",", b.Members.Select(m => $"{m.Id}={m.ConfidenceInPlayer.Value}"))));
}
