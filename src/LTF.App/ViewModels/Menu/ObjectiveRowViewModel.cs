using System;
using CommunityToolkit.Mvvm.ComponentModel;
using LTF.App.Localization;
using LTF.App.Mvvm;
using LTF.Career;
using LTF.Domain.Common;
using LTF.Domain.Management;

namespace LTF.App.ViewModels.Menu;

/// <summary>
/// One objective on the negotiating table. Holds the board's opening <em>anchor</em> and, for a given
/// ambition (a number of notches of easing), asks <see cref="BoardNegotiation"/> how the board answers and
/// converts the agreed notches into a concrete target using a per-kind step. Display-only game text is
/// built here (dynamic, number-bearing); the static reaction line comes through the localizer.
/// </summary>
public sealed partial class ObjectiveRowViewModel : ViewModelBase
{
    private readonly TeamBoard _board;
    private readonly Objective _anchor;
    private readonly int _seed;
    private readonly int _step;

    public ObjectiveRowViewModel(TeamBoard board, Objective anchor, int seed)
    {
        _board = board;
        _anchor = anchor;
        _seed = seed;
        _step = StepFor(anchor.Kind);
        _agreed = anchor;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReactionText))]
    private BoardVerdict _outcome;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AgreedText))]
    private Objective _agreed;

    /// <summary>The board's opening demand, described.</summary>
    public string ObjectiveText => Describe(_anchor);

    /// <summary>The objective as it stands after the board's answer.</summary>
    public string AgreedText => Describe(Agreed);

    public string ReactionText => Outcome switch
    {
        BoardVerdict.Accepted => Localizer.Current.Get(StringKeys.BoardReactionAccepted),
        BoardVerdict.Countered => Localizer.Current.Get(StringKeys.BoardReactionCountered),
        _ => Localizer.Current.Get(StringKeys.BoardReactionRejected),
    };

    /// <summary>Re-run the negotiation with the player asking for <paramref name="notches"/> of easing
    /// (negative = a tougher target).</summary>
    public void Apply(int notches)
    {
        var result = BoardNegotiation.Evaluate(_board, notches, _seed);
        Outcome = result.Outcome;
        Agreed = Ease(_anchor, result.AgreedNotches * _step);
    }

    private static Objective Ease(Objective o, int delta)
    {
        var target = HigherIsEasier(o.Kind) ? o.Target + delta : o.Target - delta;
        return o with { Target = Clamp(o.Kind, target) };
    }

    private static bool HigherIsEasier(ObjectiveKind kind) =>
        kind is ObjectiveKind.ConstructorPosition or ObjectiveKind.DriverPosition;

    private static int StepFor(ObjectiveKind kind) => kind switch
    {
        ObjectiveKind.ConstructorPoints => 10,
        ObjectiveKind.FinancialResult => 5_000_000,
        _ => 1,
    };

    private static int Clamp(ObjectiveKind kind, int target) => kind switch
    {
        ObjectiveKind.ConstructorPosition or ObjectiveKind.DriverPosition => Math.Clamp(target, 1, 20),
        ObjectiveKind.RaceWins or ObjectiveKind.ConstructorPoints => Math.Max(0, target),
        _ => target,
    };

    private static string Describe(Objective o) => o.Kind switch
    {
        ObjectiveKind.ConstructorPosition => $"Constructors' championship — finish P{o.Target} or better",
        ObjectiveKind.DriverPosition => $"Lead driver — finish P{o.Target} or better",
        ObjectiveKind.ConstructorPoints => $"Score at least {o.Target} constructor points",
        ObjectiveKind.RaceWins => o.Target <= 1 ? "Win a race" : $"Win at least {o.Target} races",
        ObjectiveKind.FinancialResult => o.Target switch
        {
            0 => "Break even or better",
            < 0 => $"Stay within a ${-o.Target:N0} loss",
            _ => $"End the season at least ${o.Target:N0} in profit",
        },
        _ => o.Kind.ToString(),
    };
}
