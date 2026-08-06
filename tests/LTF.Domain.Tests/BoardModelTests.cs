using LTF.Domain.Common;
using LTF.Domain.Management;
using Xunit;

namespace LTF.Domain.Tests;

public class BoardModelTests
{
    // --- Pressure (0–100) ---

    [Fact]
    public void Pressure_clamps_into_range()
    {
        Assert.Equal(Pressure.Max, Pressure.Clamped(500).Value);
        Assert.Equal(Pressure.Min, Pressure.Clamped(-500).Value);
        Assert.Equal(0, new Pressure().Value); // struct default is the floor
    }

    [Fact]
    public void Pressure_shifted_moves_and_clamps()
    {
        Assert.Equal(60, new Pressure(50).Shifted(10).Value);
        Assert.Equal(Pressure.Max, new Pressure(90).Shifted(50).Value);   // clamps at 100
        Assert.Equal(Pressure.Min, new Pressure(10).Shifted(-50).Value);  // clamps at 0
    }

    [Fact]
    public void Pressure_rejects_out_of_range()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Pressure(101));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Pressure(-1));
    }

    // --- PressureMetrics ---

    [Fact]
    public void Neutral_pressure_metrics_are_all_fifty()
    {
        var m = PressureMetrics.Neutral;
        Assert.Equal(50, m.BoardConfidence.Value);
        Assert.Equal(50, m.SportingPressure.Value);
        Assert.Equal(50, m.FinancialPressure.Value);
        Assert.Equal(50, m.SponsorPressure.Value);
        Assert.Equal(50, m.MediaPressure.Value);
        Assert.Equal(50, m.InternalPressure.Value);
    }

    // --- BoardMember ---

    [Fact]
    public void A_board_member_carries_priorities_and_sensible_defaults()
    {
        var member = new BoardMember
        {
            Id = "m1",
            SportingPriority = new(80),
            FinancialPriority = new(30),
            LongTermPriority = new(60),
            BrandPriority = new(40),
            DriverDevPriority = new(50),
            RiskTolerance = new(55),
        };

        Assert.Equal(80, member.SportingPriority.Value);
        Assert.Equal(BoardMemberTraits.None, member.Traits);
        Assert.Equal(50, member.ConfidenceInPlayer.Value); // neutral default
    }

    // --- Objective ---

    [Fact]
    public void An_objective_defaults_to_open_visibility_and_no_linkage()
    {
        var objective = new Objective { Kind = ObjectiveKind.ConstructorPosition, Target = 4 };

        Assert.Equal(ObjectiveVisibility.Open, objective.Visibility);
        Assert.Equal(4, objective.Target);
        Assert.Equal(0, objective.LinkedBudget);
        Assert.Equal(0, objective.LinkedRisk);
    }

    // --- TeamBoard defaults ---

    [Fact]
    public void A_team_board_defaults_to_neutral_pressure_and_no_members()
    {
        var board = new TeamBoard { TeamId = "talon" };

        Assert.Equal(OwnershipType.RacingOwner, board.Ownership);
        Assert.Empty(board.Members);
        Assert.Empty(board.Objectives);
        Assert.Equal(PressureMetrics.Neutral, board.Metrics);
        Assert.Equal(0, board.FiringRisk.Value);
    }

    // --- Carset default (inert) ---

    [Fact]
    public void A_carset_ships_no_boards_by_default()
    {
        Assert.Empty(Fixtures.Carset().Boards);
    }
}
