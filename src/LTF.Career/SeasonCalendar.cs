using LTF.Domain;

namespace LTF.Career;

/// <summary>The kind of thing that happens on a date in a career (ADR-0011). Race weekends are
/// built from the carset today; the other kinds are filled in by later milestones (contract
/// deadlines M12, test days M15, regulation announcements ADR-0010, transfer windows / board
/// reviews M17/M18).</summary>
public enum CalendarEventKind
{
    RaceWeekend,
    TestDay,
    RegulationAnnouncement,
    ContractDeadline,
    TransferWindow,
    BoardReview,
}

/// <summary>One dated entry in the season's event queue (ADR-0011).</summary>
public sealed record CalendarEvent
{
    public required DateOnly Date { get; init; }
    public required CalendarEventKind Kind { get; init; }

    /// <summary>A short label (e.g. the circuit id for a race weekend).</summary>
    public string Label { get; init; } = "";

    /// <summary>The championship round, for a race weekend; 0 otherwise.</summary>
    public int Round { get; init; }
}

/// <summary>
/// The season's dated event queue (ADR-0011): every event in date order. Built from the carset's
/// calendar (each round becomes a race weekend); later milestones add the other event kinds. The
/// career clock advances over this queue with "Continue".
/// </summary>
public sealed record SeasonCalendar
{
    /// <summary>Every event, ordered by date then round.</summary>
    public required IReadOnlyList<CalendarEvent> Events { get; init; }

    /// <summary>Build the queue from a carset's calendar: one race weekend per round, plus any test days
    /// (M15), in date order. Byte-identical to the M11 race-only queue when the carset ships no test days.</summary>
    public static SeasonCalendar FromCarset(Carset carset)
    {
        var events = carset.Calendar
            .OrderBy(r => r.Date)
            .ThenBy(r => r.Round)
            .Select(r => new CalendarEvent
            {
                Date = r.Date,
                Kind = CalendarEventKind.RaceWeekend,
                Label = r.CircuitId,
                Round = r.Round,
            })
            .ToList();

        events.AddRange(carset.TestDays.Select(t => new CalendarEvent
        {
            Date = t.Date,
            Kind = CalendarEventKind.TestDay,
            Label = t.CircuitId,
        }));

        var ordered = events.OrderBy(e => e.Date).ThenBy(e => e.Round).ToList();
        return new SeasonCalendar { Events = ordered };
    }

    /// <summary>Build the career calendar: the race weekends (as <see cref="FromCarset"/>) plus a
    /// contract-deadline event for each expiring contract (ADR-0011/ADR-0013), dated a short window
    /// before the final round so the player can renew before the season ends. Byte-identical to
    /// <see cref="FromCarset"/> when no contract is expiring.</summary>
    public static SeasonCalendar ForCareer(Carset carset)
    {
        var events = FromCarset(carset).Events.ToList();

        if (carset.Calendar.Count > 0)
        {
            var deadline = carset.Calendar.Max(r => r.Date).AddDays(-DeadlineDaysBeforeFinal);
            foreach (var contract in carset.Contracts)
            {
                if (contract.IsExpiring)
                {
                    events.Add(new CalendarEvent
                    {
                        Date = deadline,
                        Kind = CalendarEventKind.ContractDeadline,
                        Label = contract.PartyId,
                    });
                }
            }
        }

        var ordered = events.OrderBy(e => e.Date).ThenBy(e => e.Round).ToList();
        return new SeasonCalendar { Events = ordered };
    }

    /// <summary>Days before the season's final round that expiring contracts come up for renewal.</summary>
    private const int DeadlineDaysBeforeFinal = 7;
}
