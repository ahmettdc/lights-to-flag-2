using System;
using System.Linq;
using LTF.Domain.Racing;
using Xunit;

namespace LTF.Career.Tests;

public class TestDayCalendarTests
{
    [Fact]
    public void A_test_day_becomes_a_calendar_event()
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 2) with
        {
            TestDays = [new TestDay { Date = new DateOnly(2025, 3, 1), CircuitId = "c" }],
        };

        var calendar = SeasonCalendar.FromCarset(carset);

        var test = calendar.Events.Single(e => e.Kind == CalendarEventKind.TestDay);
        Assert.Equal(new DateOnly(2025, 3, 1), test.Date);
        Assert.Equal("c", test.Label);
        Assert.Equal(3, calendar.Events.Count); // two race weekends + one test day
    }

    [Fact]
    public void Without_test_days_the_calendar_is_race_only()
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 3);

        var calendar = SeasonCalendar.FromCarset(carset);

        Assert.Equal(3, calendar.Events.Count);
        Assert.All(calendar.Events, e => Assert.Equal(CalendarEventKind.RaceWeekend, e.Kind));
    }
}
