using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using LTF.App.Converters;
using LTF.App.Mvvm;
using LTF.App.Notifications;
using LTF.App.Services;
using LTF.App.Session;
using LTF.Career;

namespace LTF.App.ViewModels.Screens;

/// <summary>
/// The paddock hub (M21): the career dashboard. A summary column (severity counts, WCC position, cap room,
/// board confidence, the next race + a "go to race weekend" deep link), the current-events inbox snippet,
/// and a paddock-dialogue panel left as an empty state (ADR-0021 negotiation is M22/M23). Read-only over the
/// session/clock/standings/notifications; a Continue rebuilds it (M21d).
/// </summary>
public sealed partial class PaddockHubViewModel : ViewModelBase
{
    private readonly Action _goToRaceWeekend;

    public PaddockHubViewModel(ShellSession session, Standings standings, INotificationSource notifications, Action goToRaceWeekend)
    {
        _goToRaceWeekend = goToRaceWeekend;

        var snapshot = new SessionSnapshot(session);
        TeamName = snapshot.TeamName;

        HasCap = snapshot.HasCap;
        CapRoomText = HasCap ? Money(snapshot.CapRoom) : "—";

        HasBoard = snapshot.HasBoard;
        BoardConfidenceText = HasBoard ? $"{snapshot.BoardConfidence}%" : "—";
        BoardBrush = HasBoard ? StatusColorConverter.Classify(snapshot.BoardConfidence) : ScreenBrushes.Dim;

        // WCC: the player team's line in the constructors' table.
        var playerLine = session.Carset.PlayerTeamId.Length > 0
            ? standings.Constructors.FirstOrDefault(c => string.CompareOrdinal(c.TeamId, session.Carset.PlayerTeamId) == 0)
            : null;
        WccText = playerLine is not null ? $"P{playerLine.Position} · {playerLine.Points} pts" : "—";

        // Next race off the clock: the earliest round not yet in the past.
        var today = session.Clock.Date;
        var rounds = session.Carset.Calendar.OrderBy(r => r.Round).ToList();
        var circuits = session.Carset.Circuits.ToDictionary(c => c.Id, StringComparer.Ordinal);
        var next = rounds.FirstOrDefault(r => r.Date >= today);

        if (next is not null && circuits.TryGetValue(next.CircuitId, out var circuit))
        {
            HasNextRace = true;
            var days = next.Date.DayNumber - today.DayNumber;
            NextSessionText = days <= 0 ? "Race day" : days == 1 ? "Race · in 1 day" : $"Race · in {days} days";
            NextEventName = circuit.Name.ToUpperInvariant();
            NextEventSubtitle = $"Round {next.Round:00} / {rounds.Count} · {circuit.Name}";
        }
        else
        {
            NextSessionText = "—";
            NextEventName = "SEASON COMPLETE";
            NextEventSubtitle = "";
        }

        // Inbox snippet + severity roll-up.
        var items = notifications.Current();
        CriticalCount = items.Count(n => n.Severity == NotificationSeverity.Critical);
        WarningCount = items.Count(n => n.Severity == NotificationSeverity.Warning);
        PendingCount = items.Count(n => n.Severity is NotificationSeverity.Info or NotificationSeverity.Success);
        InboxCountText = $"INBOX · {items.Count} NEW";
        Events = items.Select(n => new PaddockEventViewModel(
            n.Title,
            n.Category.ToString().ToUpperInvariant(),
            n.Body,
            Dot(n.Severity))).ToList();
    }

    public string TeamName { get; }

    public bool HasCap { get; }

    public string CapRoomText { get; }

    public bool HasBoard { get; }

    public string BoardConfidenceText { get; }

    public IBrush BoardBrush { get; }

    public string WccText { get; }

    public bool HasNextRace { get; }

    public string NextSessionText { get; }

    public string NextEventName { get; }

    public string NextEventSubtitle { get; }

    public int CriticalCount { get; }

    public int WarningCount { get; }

    public int PendingCount { get; }

    public string InboxCountText { get; }

    public IReadOnlyList<PaddockEventViewModel> Events { get; }

    [RelayCommand]
    private void GoToRaceWeekend() => _goToRaceWeekend();

    private static IBrush Dot(NotificationSeverity severity) => severity switch
    {
        NotificationSeverity.Success => SeverityColorConverter.Success,
        NotificationSeverity.Warning => SeverityColorConverter.Warning,
        NotificationSeverity.Critical => SeverityColorConverter.Critical,
        _ => SeverityColorConverter.Info,
    };

    private static string Money(long value)
    {
        var abs = Math.Abs(value);
        return abs >= 1_000_000
            ? $"${value / 1_000_000.0:0.#}M"
            : $"${value / 1_000.0:0}k";
    }
}

/// <summary>One row in the paddock hub's current-events snippet (M21).</summary>
public sealed record PaddockEventViewModel(string Title, string Category, string Body, IBrush DotBrush);
