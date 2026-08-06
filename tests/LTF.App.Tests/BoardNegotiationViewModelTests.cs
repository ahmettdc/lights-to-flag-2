using LTF.App.ViewModels.Menu;
using LTF.Career;
using LTF.Domain.Common;
using LTF.Domain.Management;
using Xunit;

namespace LTF.App.Tests;

/// <summary>
/// The board-negotiation view-model (M20c): ambition drives every objective through the deterministic
/// <see cref="BoardNegotiation"/>, converting the board's answer into a concrete target. Pure VM logic.
/// </summary>
public class BoardNegotiationViewModelTests
{
    private static TeamBoard PositionBoard(OwnershipType ownership, int target) => new()
    {
        TeamId = "t",
        Ownership = ownership,
        Objectives = [new Objective { Kind = ObjectiveKind.ConstructorPosition, Target = target }],
    };

    [Fact]
    public void Balanced_ambition_keeps_the_board_anchor()
    {
        var vm = new BoardNegotiationViewModel(PositionBoard(OwnershipType.DevelopmentProject, 3), seed: 1);

        Assert.Equal(BoardAmbition.Balanced, vm.Ambition);
        Assert.Equal(3, vm.Objectives[0].Agreed.Target);
        Assert.Single(vm.Agreed);
    }

    [Fact]
    public void Cautious_ambition_eases_a_position_target_a_lenient_board_allows()
    {
        var vm = new BoardNegotiationViewModel(PositionBoard(OwnershipType.DevelopmentProject, 3), seed: 1);

        vm.Ambition = BoardAmbition.Cautious;

        // A development board grants 2 notches → P3 becomes P5, accepted.
        Assert.Equal(BoardVerdict.Accepted, vm.Objectives[0].Outcome);
        Assert.Equal(5, vm.Objectives[0].Agreed.Target);
    }

    [Fact]
    public void Aggressive_ambition_commits_to_a_tougher_position_target()
    {
        var vm = new BoardNegotiationViewModel(PositionBoard(OwnershipType.RacingOwner, 3), seed: 1);

        vm.Ambition = BoardAmbition.Aggressive;

        // A tougher target is always accepted → P3 becomes P2.
        Assert.Equal(BoardVerdict.Accepted, vm.Objectives[0].Outcome);
        Assert.Equal(2, vm.Objectives[0].Agreed.Target);
    }

    [Fact]
    public void Synthesises_a_default_objective_when_the_board_ships_none()
    {
        var vm = new BoardNegotiationViewModel(new TeamBoard { TeamId = "t" }, seed: 1);

        Assert.Single(vm.Objectives);
        Assert.NotEmpty(vm.Agreed);
    }
}
