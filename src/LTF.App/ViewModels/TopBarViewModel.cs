using System;
using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using LTF.App.Localization;
using LTF.App.Mvvm;
using LTF.App.Services;

namespace LTF.App.ViewModels;

/// <summary>
/// The top bar's data: the live date, cost-cap room, board confidence and team badge from the session
/// snapshot, plus the inbox count. Continue is present but inert in M19 — the per-event runner is M21.
/// The INBOX button toggles the shell's inbox popover via an optional callback the shell supplies.
/// </summary>
public sealed partial class TopBarViewModel : ViewModelBase
{
    private ISessionSnapshot _session;
    private readonly Action? _onToggleInbox;
    private readonly Action? _onContinue;
    private readonly Func<bool>? _canContinue;

    public TopBarViewModel(
        ISessionSnapshot session,
        int inboxCount = 0,
        Action? onToggleInbox = null,
        Action? onContinue = null,
        Func<bool>? canContinue = null)
    {
        _session = session;
        InboxCount = inboxCount;
        _onToggleInbox = onToggleInbox;
        _onContinue = onContinue;
        _canContinue = canContinue;
    }

    /// <summary>Re-read the top bar from a fresh snapshot after a Continue (the date and, once the carset
    /// evolves, cap/board move). Raises change for every snapshot-derived value and re-evaluates Continue.</summary>
    public void Refresh(ISessionSnapshot session)
    {
        _session = session;
        OnPropertyChanged(string.Empty);
        ContinueCommand.NotifyCanExecuteChanged();
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

    // Localised chrome labels (English pack today; a Turkish pack slots in behind Localizer — ADR-0026).
    public string ContinueLabel => $"{Localizer.Current.Get(StringKeys.TopContinue)} »";

    public string InboxLabel => Localizer.Current.Get(StringKeys.TopInbox);

    public string SearchPlaceholder => Localizer.Current.Get(StringKeys.TopSearchPlaceholder);

    public string CapRoomLabel => Localizer.Current.Get(StringKeys.TopCapRoom);

    public string BoardConfLabel => Localizer.Current.Get(StringKeys.TopBoardConf);

    /// <summary>Whether Continue can advance the career (false once the season is complete).</summary>
    public bool CanContinue => _canContinue?.Invoke() ?? true;

    // The per-event Continue runner (M21): advance the live career via the host-supplied callback. Inert only
    // when no callback is wired (e.g. a snapshot-only shell in tests).
    [RelayCommand(CanExecute = nameof(CanContinue))]
    private void Continue() => _onContinue?.Invoke();

    [RelayCommand]
    private void ToggleInbox() => _onToggleInbox?.Invoke();

    private static string FormatMoney(long amount) =>
        string.Create(CultureInfo.InvariantCulture, $"${amount / 1_000_000.0:0.0}M");
}
