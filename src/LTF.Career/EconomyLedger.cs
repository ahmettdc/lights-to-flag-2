using LTF.Domain;
using LTF.Domain.Common;
using LTF.Domain.Management;
using LTF.Domain.Racing;
using LTF.Simulation.Racing;

namespace LTF.Career;

/// <summary>
/// Settles a team's finances at season rollover (M13): it credits the income — prize money by
/// constructors'-championship position, flat TV income, and sponsor fees and bonuses — and subtracts
/// the expenses — driver and staff salaries (with per-point and title bonuses) plus per-race operating
/// cost and per-collision crash cost — leaving the net on the balance. Pure, deterministic arithmetic
/// over the finished <see cref="SeasonResult"/> (no RNG), so the same carset and season always reach
/// the same books. With an empty economy no money moves and the carset is untouched.
/// </summary>
public static class EconomyLedger
{
    /// <summary>Return the carset with every team's finances settled for the season.</summary>
    public static Carset SettleSeason(Carset carset, SeasonResult season)
    {
        var teams = carset.Teams
            .Select(t => t with { Finances = Settle(carset, season, t) })
            .ToList();
        return carset with { Teams = teams };
    }

    private static Finances Settle(Carset carset, SeasonResult season, Team team)
    {
        var standing = StandingOf(season, team.Id);
        var position = standing?.Position ?? 0;
        var points = standing?.Points ?? 0;
        var races = season.Rounds.Count;
        var economy = carset.Rules.Economy;

        var prize = economy.PrizeFor(position);
        var tv = economy.TvIncome;
        var sponsorIncome = SponsorIncome(team, races, points, position);
        var income = prize + tv + sponsorIncome;
        var expense = Expense(carset, season, team, races);

        return team.Finances with
        {
            Balance = team.Finances.Balance + income - expense,
            PrizeMoney = prize,
            SponsorIncome = sponsorIncome,
        };
    }

    private static long Expense(Carset carset, SeasonResult season, Team team, int races)
    {
        var economy = carset.Rules.Economy;

        long driverSalaries = 0;
        foreach (var contract in carset.Contracts)
        {
            if (contract.Kind != ContractKind.Driver || contract.TeamId != team.Id)
            {
                continue;
            }

            driverSalaries += contract.SalaryPerSeason
                + contract.Clauses.PerPointBonus * DriverPoints(season, contract.PartyId);
            if (season.DriversChampionId == contract.PartyId)
            {
                driverSalaries += contract.Clauses.ChampionshipBonus;
            }
        }

        long staffSalaries = 0;
        foreach (var member in team.Staff)
        {
            staffSalaries += member.Salary;
        }

        var operating = economy.OperatingCostPerRace * races;
        var crash = economy.CrashCostPerIncident * CrashCount(season, team);

        return driverSalaries + staffSalaries + operating + crash;
    }

    private static int DriverPoints(SeasonResult season, string driverId)
    {
        foreach (var driver in season.Standings.Drivers)
        {
            if (driver.DriverId == driverId)
            {
                return driver.Points;
            }
        }

        return 0;
    }

    // The number of collisions a team's cars were involved in across the season (each involved car
    // books its own repair) — the crash-cost driver.
    private static int CrashCount(SeasonResult season, Team team)
    {
        var teamDrivers = new HashSet<string>(team.DriverIds, StringComparer.Ordinal);
        var count = 0;
        foreach (var round in season.Rounds)
        {
            foreach (var raceEvent in round.Events)
            {
                if (raceEvent.Kind != RaceEventKind.Collision)
                {
                    continue;
                }

                if (teamDrivers.Contains(raceEvent.CompetitorId))
                {
                    count++;
                }

                if (raceEvent.OtherCompetitorId is { } other && teamDrivers.Contains(other))
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static long SponsorIncome(Team team, int races, int points, int position)
    {
        long total = 0;
        foreach (var sponsor in team.Sponsors)
        {
            total += sponsor.PerRaceFee * races + sponsor.PerPointBonus * points;
            if (sponsor.ObjectivePosition > 0 && position > 0 && position <= sponsor.ObjectivePosition)
            {
                total += sponsor.ObjectiveBonus;
            }
        }

        return total;
    }

    private static ConstructorStanding? StandingOf(SeasonResult season, string teamId)
    {
        foreach (var constructor in season.Standings.Constructors)
        {
            if (constructor.TeamId == teamId)
            {
                return constructor;
            }
        }

        return null;
    }
}
