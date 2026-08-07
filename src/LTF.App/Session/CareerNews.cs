using System;
using System.Globalization;
using System.Linq;
using LTF.App.Navigation;
using LTF.App.Notifications;
using LTF.Career;
using LTF.Domain;
using LTF.Domain.Racing;
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

    private static string DriverName(Carset carset, string id)
    {
        var driver = carset.Drivers.FirstOrDefault(d => string.CompareOrdinal(d.Id, id) == 0);
        return driver?.FullName ?? id;
    }

    private static string CircuitName(Carset carset, string id)
    {
        var circuit = carset.Circuits.FirstOrDefault(c => string.CompareOrdinal(c.Id, id) == 0);
        return circuit?.Name ?? id;
    }
}
