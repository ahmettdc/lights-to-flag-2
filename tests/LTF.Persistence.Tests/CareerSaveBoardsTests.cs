using System.Linq;
using LTF.Career;
using LTF.Domain;
using LTF.Domain.Common;
using LTF.Domain.Management;
using LTF.Domain.Racing;
using Xunit;

namespace LTF.Persistence.Tests;

public class CareerSaveBoardsTests
{
    private static CareerState Sample() => new()
    {
        Seed = 17,
        Date = new DateOnly(2025, 11, 1),
        PlayerTeamId = "alpha",
        Boards =
        [
            new TeamBoardRecord
            {
                TeamId = "alpha",
                Ownership = OwnershipType.ManufacturerBacked,
                BoardConfidence = 72, SportingPressure = 40, FinancialPressure = 25,
                SponsorPressure = 30, MediaPressure = 35, InternalPressure = 20,
                FiringRisk = 15,
                Members =
                [
                    new BoardMemberRecord
                    {
                        Id = "chair", Name = "The Chair",
                        SportingPriority = 80, FinancialPriority = 30, LongTermPriority = 60,
                        BrandPriority = 40, DriverDevPriority = 50, ConfidenceInPlayer = 68, RiskTolerance = 55,
                        Traits = BoardMemberTraits.Impatient | BoardMemberTraits.Ambitious,
                    },
                ],
                Objectives =
                [
                    new Objective
                    {
                        Kind = ObjectiveKind.ConstructorPosition, Visibility = ObjectiveVisibility.Open,
                        Target = 2, LinkedBudget = 5_000_000, LinkedRisk = 30,
                    },
                ],
            },
            new TeamBoardRecord { TeamId = "bravo" }, // no state → defaults
        ],
    };

    [Fact]
    public void A_round_trip_preserves_the_player_team_and_boards()
    {
        var loaded = CareerStore.Deserialize(CareerStore.Serialize(Sample()));

        Assert.Equal("alpha", loaded.PlayerTeamId);

        var alpha = loaded.Boards.Single(b => b.TeamId == "alpha");
        Assert.Equal(OwnershipType.ManufacturerBacked, alpha.Ownership);
        Assert.Equal(72, alpha.BoardConfidence);
        Assert.Equal(20, alpha.InternalPressure);
        Assert.Equal(15, alpha.FiringRisk);

        var chair = alpha.Members.Single();
        Assert.Equal("chair", chair.Id);
        Assert.Equal(80, chair.SportingPriority);
        Assert.Equal(68, chair.ConfidenceInPlayer);
        Assert.Equal(BoardMemberTraits.Impatient | BoardMemberTraits.Ambitious, chair.Traits);

        var objective = alpha.Objectives.Single();
        Assert.Equal(ObjectiveKind.ConstructorPosition, objective.Kind);
        Assert.Equal(2, objective.Target);
        Assert.Equal(5_000_000L, objective.LinkedBudget);

        Assert.Empty(loaded.Boards.Single(b => b.TeamId == "bravo").Members); // defaults round-trip empty
    }

    [Fact]
    public void Serialization_stays_byte_stable_with_boards()
    {
        var once = CareerStore.Serialize(Sample());
        var twice = CareerStore.Serialize(CareerStore.Deserialize(once));

        Assert.Equal(once, twice);
    }

    [Fact]
    public void Capture_then_restore_carries_boards_back_to_the_carset()
    {
        var played = BoardCarset(); // alpha carries an evolved board

        var state = CareerState.Capture(played, new DateOnly(2025, 9, 1), 7);

        // A fresh copy with the board wiped back to a neutral default.
        var fresh = played with { Boards = [new TeamBoard { TeamId = "alpha" }] };
        Assert.Equal(50, fresh.Boards[0].Metrics.BoardConfidence.Value); // sanity: really neutral

        var resumed = state.RestoreInto(fresh);
        var board = resumed.Boards.Single();

        Assert.Equal("alpha", resumed.PlayerTeamId);
        Assert.Equal(OwnershipType.FinanceBoard, board.Ownership);
        Assert.Equal(72, board.Metrics.BoardConfidence.Value);
        Assert.Equal(15, board.FiringRisk.Value);
        Assert.Equal(68, board.Members.Single().ConfidenceInPlayer.Value);
        Assert.Equal(ObjectiveKind.RaceWins, board.Objectives.Single().Kind);
    }

    [Fact]
    public void A_carset_without_boards_captures_none()
    {
        var noBoards = BoardCarset() with { Boards = [] };

        var state = CareerState.Capture(noBoards, new DateOnly(2025, 9, 1), 7);

        Assert.Empty(state.Boards);
    }

    private static Carset BoardCarset() => new()
    {
        Id = "mini",
        Name = "Mini",
        Rules = new RulesSet { SeriesName = "S", Points = new PointsScheme { RacePoints = [25, 18] } },
        Teams = [new Team { Id = "alpha", Name = "Alpha", DriverIds = ["d1"], Car = Flat(70) }],
        Drivers = [new Driver { Id = "d1", FirstName = "Ada", LastName = "One", Age = 24, Attributes = Attrs() }],
        Circuits = [],
        Calendar = [],
        PlayerTeamId = "alpha",
        Boards =
        [
            new TeamBoard
            {
                TeamId = "alpha",
                Ownership = OwnershipType.FinanceBoard,
                Metrics = new PressureMetrics
                {
                    BoardConfidence = new(72), SportingPressure = new(40), FinancialPressure = new(25),
                    SponsorPressure = new(30), MediaPressure = new(35), InternalPressure = new(20),
                },
                FiringRisk = new(15),
                Members =
                [
                    new BoardMember
                    {
                        Id = "chair", SportingPriority = new(80), FinancialPriority = new(30),
                        LongTermPriority = new(60), BrandPriority = new(40), DriverDevPriority = new(50),
                        ConfidenceInPlayer = new(68), RiskTolerance = new(55),
                    },
                ],
                Objectives = [new Objective { Kind = ObjectiveKind.RaceWins, Target = 3 }],
            },
        ],
    };

    private static Car Flat(int v) => new()
    {
        Aerodynamics = new(v), Chassis = new(v), PowerUnit = new(v), TyreGentleness = new(v), Reliability = new(v),
    };

    private static DriverAttributes Attrs() => new()
    {
        Pace = new(70), Racecraft = new(70), Consistency = new(70),
        TyreManagement = new(70), WetWeather = new(70), Feedback = new(70),
    };
}
