using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using LTF.App.Mvvm;
using LTF.App.Services;

namespace LTF.App.ViewModels;

/// <summary>
/// The top bar's data: the live date, cost-cap room, board confidence and team badge from the session
/// snapshot, plus the inbox count. Continue is present but inert in M19 — the per-event runner is M21.
/// </summary>
public sealed partial class TopBarViewModel : ViewModelBase
{
    private readonly ISessionSnapshot _session;

    public TopBarViewModel(ISessionSnapshot session, int inboxCount = 0)
    {
        _session = session;
        InboxCount = inboxCount;
    }

    /// <summary>Date shown as e.g. "14 MAY 2027" (current culture for the month, invariant upper-casing).</summary>
    public string DateText =>
        _session.Date.ToString("dd MMM yyyy", CultureInfo.CurrentCulture).ToUpperInvariant();

    public bool HasPlayerTeam => _session.HasPlayerTeam;

    public string TeamName => _session.TeamName;

    public string TeamBadge => _session.TeamBadge;

    public bool HasCap => _session.HasCap;

    public string CapRoomText => FormatMoney(_session.CapRoom);

    public bool HasBoard => _session.HasBoard;

    public int BoardConfidence => _session.BoardConfidence;

    public string BoardConfidenceText =>
        string.Create(CultureInfo.InvariantCulture, $"{_session.BoardConfidence}%");

    public int InboxCount { get; }

    public bool HasInbox => InboxCount > 0;

    [RelayCommand]
    private void Continue()
    {
        // Inert in M19 — the per-event Continue runner arrives in M21.
    }

    private static string FormatMoney(long amount) =>
        string.Create(CultureInfo.InvariantCulture, $"${amount / 1_000_000.0:0.0}M");
}
