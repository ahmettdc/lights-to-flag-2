using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LTF.App.Mvvm;
using LTF.Career;
using LTF.Domain.Management;

namespace LTF.App.ViewModels.Menu;

/// <summary>The player's mandate ambition — how hard they push the board on its objectives.</summary>
public enum BoardAmbition
{
    /// <summary>Ask the board to ease its objectives.</summary>
    Cautious,

    /// <summary>Take the board's objectives as offered.</summary>
    Balanced,

    /// <summary>Commit to tougher objectives than the board asked for.</summary>
    Aggressive,
}

/// <summary>
/// The interactive board-objective negotiation (M20c). Presents each of the board's objectives and lets the
/// player pick an ambition; every objective is re-negotiated through <see cref="BoardNegotiation"/> against
/// that ambition, and the board answers per objective. <see cref="Agreed"/> is what the career starts with.
/// </summary>
public sealed partial class BoardNegotiationViewModel : ViewModelBase
{
    public BoardNegotiationViewModel(TeamBoard board, int seed)
    {
        var anchors = board.Objectives.Count > 0
            ? board.Objectives.ToList()
            : new List<Objective> { BoardNegotiation.Opening(board) };

        Objectives = new ObservableCollection<ObjectiveRowViewModel>(
            anchors.Select((anchor, i) => new ObjectiveRowViewModel(board, anchor, seed + i)));

        ApplyAmbition();
    }

    public ObservableCollection<ObjectiveRowViewModel> Objectives { get; }

    [ObservableProperty]
    private BoardAmbition _ambition = BoardAmbition.Balanced;

    /// <summary>The objectives as agreed after the board's answer to the current ambition.</summary>
    public IReadOnlyList<Objective> Agreed => Objectives.Select(o => o.Agreed).ToList();

    partial void OnAmbitionChanged(BoardAmbition value) => ApplyAmbition();

    [RelayCommand]
    private void Cautious() => Ambition = BoardAmbition.Cautious;

    [RelayCommand]
    private void Balanced() => Ambition = BoardAmbition.Balanced;

    [RelayCommand]
    private void Aggressive() => Ambition = BoardAmbition.Aggressive;

    private void ApplyAmbition()
    {
        var notches = Ambition switch
        {
            BoardAmbition.Cautious => 2,
            BoardAmbition.Aggressive => -1,
            _ => 0,
        };

        foreach (var row in Objectives)
        {
            row.Apply(notches);
        }
    }
}
