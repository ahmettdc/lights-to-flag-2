using LTF.Domain;

namespace LTF.Career;

/// <summary>
/// The Football-Manager-style game clock (ADR-0011): a game date over the season's event queue.
/// The date is game state — never the wall clock (no <c>DateTime.Now</c>) — so the same career
/// plays out identically. "Continue" jumps to the next dated event, skipping empty days; a single
/// day can also be stepped. Immutable: every advance returns the next clock.
/// </summary>
public sealed record CareerClock
{
    public required DateOnly Date { get; init; }
    public required SeasonCalendar Calendar { get; init; }

    /// <summary>The events falling on the current date (what needs handling "today").</summary>
    public IReadOnlyList<CalendarEvent> Today => Calendar.Events.Where(e => e.Date == Date).ToList();

    /// <summary>The earliest event strictly after the current date, or null if none remain.</summary>
    public CalendarEvent? NextEvent => Calendar.Events.FirstOrDefault(e => e.Date > Date);

    /// <summary>True once no events remain after the current date — you are on or past the last one,
    /// so "Continue" has nowhere left to go.</summary>
    public bool SeasonComplete => NextEvent is null;

    /// <summary>Step one day forward.</summary>
    public CareerClock AdvanceDay() => this with { Date = Date.AddDays(1) };

    /// <summary>"Continue": jump to the next event's date, or stay put if none remain.</summary>
    public CareerClock ContinueToNextEvent()
    {
        var next = NextEvent;
        return next is null ? this : this with { Date = next.Date };
    }

    /// <summary>Open a career on a carset: the clock starts the day before the first event, so the
    /// first "Continue" lands on it. An empty calendar opens on the first of the millennium's
    /// reference year (a fixed, deterministic default).</summary>
    public static CareerClock Start(Carset carset)
    {
        var calendar = SeasonCalendar.ForCareer(carset);
        var open = calendar.Events.Count > 0
            ? calendar.Events[0].Date.AddDays(-1)
            : new DateOnly(2000, 1, 1);
        return new CareerClock { Date = open, Calendar = calendar };
    }
}
