using System;
using System.Globalization;
using System.Linq;
using LTF.App.Navigation;
using LTF.App.Notifications;
using LTF.Career;
using LTF.Domain;
using LTF.Domain.Racing;
using LTF.Domain.Rnd;
using LTF.Simulation.Racing;

namespace LTF.App.Session;

/// <summary>
/// Turns a dispatched career event into a dated inbox <see cref="Notification"/> (M21). A race becomes a
/// result item deep-linking the standings; a contract deadline becomes an <em>action-required</em> item
/// that pauses Continue (Rev 15); a board review is informational. Names are resolved off the carset.
/// </summary>
internal static class CareerNews
{
    public static Notification ForRace(DateOnly date, CalendarRound round, RaceResult result, Carset carset)
    {
        var winnerId = result.Classification.Where(e => e.Position == 1).Select(e => e.CompetitorId).FirstOrDefault();
        var winner = winnerId is not null ? DriverName(carset, winnerId) : "The field";
        var circuit = CircuitName(carset, round.CircuitId);

        return new Notification(
            $"race-{round.Round}",
            NotificationCategory.Race,
            NotificationSeverity.Success,
            $"Round {round.Round}: {circuit}",
            $"{winner} won at {circuit}.",
            NavKey.Standings,
            date);
    }

    public static Notification ForContractDeadline(DateOnly date, CalendarEvent contractEvent, Carset carset)
    {
        var party = DriverName(carset, contractEvent.Label);

        return new Notification(
            $"deadline-{contractEvent.Label}-{date.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}",
            NotificationCategory.Staff,
            NotificationSeverity.Warning,
            $"Contract deadline: {party}",
            $"{party}'s contract is up for renewal before the season ends.",
            NavKey.Drivers,
            date,
            RequiresAction: true);
    }

    public static Notification ForNewSeason(DateOnly date, int seasonNumber) =>
        new(
            $"season-{seasonNumber}",
            NotificationCategory.Press,
            NotificationSeverity.Info,
            "A new season begins",
            "The grid resets and a fresh championship gets under way.",
            NavKey.PaddockHub,
            date);

    public static Notification ForBoardReview(DateOnly date) =>
        new(
            $"board-{date.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}",
            NotificationCategory.Board,
            NotificationSeverity.Info,
            "Board review",
            "The board has reviewed the team's progress this season.",
            NavKey.PaddockHub,
            date);

    /// <summary>A bank enforcement escalation (ADR-0029): a missed instalment, a forced asset sale, or the
    /// terminal insolvency penalty. Always <em>action-required</em> — the creditors are on your tail, so
    /// Continue halts until the player acknowledges (Rev 15).</summary>
    public static Notification ForEnforcement(DateOnly date, EnforcementAction action, Carset carset)
    {
        var team = TeamName(carset, action.TeamId);
        var (title, body, severity) = action.Step switch
        {
            EnforcementStep.Insolvency => (
                "Insolvency enforcement",
                $"Sustained default: the bank docked {action.PointsDocked} points and liquidated assets. Clear the debt to call off the bailiffs.",
                NotificationSeverity.Critical),
            EnforcementStep.AssetSeizure => (
                "Assets seized",
                action.SeizedAsset.Length > 0
                    ? $"Creditors forced the sale of {action.SeizedAsset} to service {team}'s debt."
                    : $"Creditors moved to seize {team}'s assets over the unpaid loan.",
                NotificationSeverity.Warning),
            _ => (
                "Creditors are circling",
                $"{team} missed a loan instalment — the bank has issued a formal warning.",
                NotificationSeverity.Warning),
        };

        return new Notification(
            $"enforce-{action.TeamId}-{action.Step}-{date.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}",
            NotificationCategory.Finance,
            severity,
            title,
            body,
            NavKey.Finance,
            date,
            RequiresAction: true);
    }

    /// <summary>An FIA development-freeze change (Ri4): the regulation ballot has frozen — or lifted — in-season
    /// development on one or more car axes for the coming season. Informational; deep-links the R&amp;D screen.</summary>
    public static Notification ForFreeze(DateOnly date, System.Collections.Generic.IReadOnlyList<AxisFreeze> freezes)
    {
        var lifted = freezes.Count == 0;
        var body = lifted
            ? "The FIA has lifted its development freeze — every axis develops freely again."
            : $"The FIA has frozen development on: {string.Join(", ", freezes.Select(f => f.Axis))}.";

        return new Notification(
            $"freeze-{date.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}",
            NotificationCategory.Press,
            NotificationSeverity.Info,
            lifted ? "Development freeze lifted" : "Development freeze in force",
            body,
            NavKey.RndFacilities,
            date);
    }

    private static string DriverName(Carset carset, string id)
    {
        var driver = carset.Drivers.FirstOrDefault(d => string.CompareOrdinal(d.Id, id) == 0);
        return driver?.FullName ?? id;
    }

    private static string TeamName(Carset carset, string id)
    {
        var team = carset.Teams.FirstOrDefault(t => string.CompareOrdinal(t.Id, id) == 0);
        return team?.Name ?? id;
    }

    private static string CircuitName(Carset carset, string id)
    {
        var circuit = carset.Circuits.FirstOrDefault(c => string.CompareOrdinal(c.Id, id) == 0);
        return circuit?.Name ?? id;
    }
}
