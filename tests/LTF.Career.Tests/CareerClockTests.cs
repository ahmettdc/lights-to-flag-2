using System.Collections.Generic;
using System.Linq;
using LTF.Domain;
using Xunit;

namespace LTF.Career.Tests;

public class CareerClockTests
{
    [Fact]
    public void The_calendar_is_built_from_the_carsets_rounds()
    {
        var calendar = SeasonCalendar.FromCarset(CareerFixtures.SeasonCarset(rounds: 5));

        Assert.Equal(5, calendar.Events.Count);
        Assert.All(calendar.Events, e => Assert.Equal(CalendarEventKind.RaceWeekend, e.Kind));
        var dates = calendar.Events.Select(e => e.Date).ToList();
        Assert.Equal(dates.OrderBy(d => d), dates);
    }

    [Fact]
    public void Continue_advances_to_the_next_event()
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 3);
        var clock = CareerClock.Start(carset);

        var first = clock.ContinueToNextEvent();
        var second = first.ContinueToNextEvent();

        Assert.Equal(carset.Calendar[0].Date, first.Date);
        Assert.Equal(carset.Calendar[1].Date, second.Date);
    }

    [Fact]
    public void Advance_day_steps_one_day()
    {
        var clock = CareerClock.Start(CareerFixtures.SeasonCarset());

        Assert.Equal(clock.Date.AddDays(1), clock.AdvanceDay().Date);
    }

    [Fact]
    public void Today_lists_the_events_on_the_current_date()
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 2);
        var clock = CareerClock.Start(carset).ContinueToNextEvent();

        Assert.Single(clock.Today);
        Assert.Equal(CalendarEventKind.RaceWeekend, clock.Today[0].Kind);
        Assert.Equal(carset.Calendar[0].Round, clock.Today[0].Round);
    }

    [Fact]
    public void Continue_stops_at_the_end_of_the_season()
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 3);
        var clock = CareerClock.Start(carset);
        for (var i = 0; i < 3; i++)
        {
            clock = clock.ContinueToNextEvent();
        }

        Assert.Equal(carset.Calendar[2].Date, clock.Date); // on the last event
        Assert.True(clock.SeasonComplete);                 // nothing scheduled after it

        var after = clock.ContinueToNextEvent();
        Assert.Equal(clock.Date, after.Date);              // Continue is a no-op past the end
    }

    [Fact]
    public void The_clock_is_deterministic()
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 4);

        Assert.Equal(Walk(carset), Walk(carset));
    }

    private static IReadOnlyList<DateOnly> Walk(Carset carset)
    {
        var clock = CareerClock.Start(carset);
        var dates = new List<DateOnly> { clock.Date };
        while (!clock.SeasonComplete)
        {
            clock = clock.ContinueToNextEvent();
            dates.Add(clock.Date);
        }

        return dates;
    }
}
