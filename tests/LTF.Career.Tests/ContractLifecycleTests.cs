using System.Linq;
using LTF.Domain;
using LTF.Domain.Common;
using LTF.Domain.Management;
using Xunit;

namespace LTF.Career.Tests;

public class ContractLifecycleTests
{
    private static Carset WithContracts(int rounds, params (string PartyId, int Seasons)[] contracts)
    {
        var carset = CareerFixtures.SeasonCarset(rounds: rounds);
        var list = contracts
            .Select(c => new Contract
            {
                Kind = ContractKind.Driver,
                PartyId = c.PartyId,
                TeamId = "alpha",
                SalaryPerSeason = 1_000_000,
                SeasonsRemaining = c.Seasons,
            })
            .ToList();
        return carset with { Contracts = list };
    }

    [Fact]
    public void Contract_terms_count_down_at_rollover()
    {
        var carset = WithContracts(3, ("d1", 3));

        var after = ContractLedger.AdvanceSeason(carset);

        Assert.Equal(2, after.Contracts.Single(c => c.PartyId == "d1").SeasonsRemaining);
    }

    [Fact]
    public void A_contract_in_its_final_season_lapses()
    {
        var carset = WithContracts(3, ("d1", 1), ("d2", 2));

        var after = ContractLedger.AdvanceSeason(carset);

        Assert.DoesNotContain(after.Contracts, c => c.PartyId == "d1"); // served its term, dropped
        Assert.Equal(1, after.Contracts.Single(c => c.PartyId == "d2").SeasonsRemaining);
    }

    [Fact]
    public void Advancing_a_contract_free_carset_is_inert()
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 3); // no contracts

        Assert.Empty(ContractLedger.AdvanceSeason(carset).Contracts);
    }

    [Fact]
    public void An_expiring_contract_adds_a_dated_deadline_to_the_career_calendar()
    {
        var carset = WithContracts(3, ("d1", 1)); // expiring (SeasonsRemaining <= 1)

        var calendar = SeasonCalendar.ForCareer(carset);

        var deadline = carset.Calendar.Max(r => r.Date).AddDays(-7);
        Assert.Contains(calendar.Events, e =>
            e.Kind == CalendarEventKind.ContractDeadline && e.Label == "d1" && e.Date == deadline);
    }

    [Fact]
    public void A_non_expiring_contract_adds_no_deadline()
    {
        var carset = WithContracts(3, ("d1", 3)); // not expiring

        var calendar = SeasonCalendar.ForCareer(carset);

        Assert.DoesNotContain(calendar.Events, e => e.Kind == CalendarEventKind.ContractDeadline);
    }

    [Fact]
    public void Without_expiring_contracts_the_career_calendar_matches_the_race_calendar()
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 5); // no contracts

        var career = SeasonCalendar.ForCareer(carset);
        var race = SeasonCalendar.FromCarset(carset);

        Assert.Equal(race.Events.Count, career.Events.Count);
        Assert.All(career.Events, e => Assert.Equal(CalendarEventKind.RaceWeekend, e.Kind));
    }

    [Fact]
    public void The_career_clock_surfaces_a_contract_deadline()
    {
        var carset = WithContracts(3, ("d1", 1));

        var clock = CareerClock.Start(carset);
        while (!clock.SeasonComplete && clock.Today.All(e => e.Kind != CalendarEventKind.ContractDeadline))
        {
            clock = clock.ContinueToNextEvent();
        }

        Assert.Contains(clock.Today, e => e.Kind == CalendarEventKind.ContractDeadline && e.Label == "d1");
    }
}
