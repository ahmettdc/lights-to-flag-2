using LTF.Domain;
using LTF.Domain.Common;
using LTF.Domain.Management;
using LTF.Domain.Racing;
using LTF.Simulation.Racing;

namespace LTF.Career;

/// <summary>
/// Settles a team's finances at season rollover (M13): it credits the income — prize money by
/// constructors'-championship position, flat TV income, and sponsor fees and bonuses — subtracts the
/// expenses — driver and staff salaries (with per-point and title bonuses) plus per-race operating cost
/// and per-collision crash cost — and, where a series runs a cost cap, raises a <see cref="CostCapPenalty"/>
/// for any team whose capped spending (staff, operating, crash and any R&amp;D spend a career folds in —
/// driver salaries sit outside the cap) overran the cap, deducting the fine from its balance. Pure, deterministic arithmetic over the finished
/// <see cref="SeasonResult"/> (no RNG), so the same carset and season always reach the same books. With an
/// empty economy no money moves, no penalty is raised, and the carset is untouched.
/// </summary>
public static class EconomyLedger
{
    /// <summary>Settle every team's finances for the season and collect any cost-cap penalties. A career
    /// may pass each team's R&amp;D spend so it counts against the cost cap (M17); absent, no R&amp;D is folded in
    /// and the books are byte-identical to M13.</summary>
    public static SeasonSettlement SettleSeason(
        Carset carset, SeasonResult season, IReadOnlyDictionary<string, long>? rndSpend = null)
    {
        var penalties = new List<CostCapPenalty>();
        var teams = new List<Team>(carset.Teams.Count);
        foreach (var team in carset.Teams)
        {
            var (finances, penalty) = Settle(carset, season, team, rndSpend);
            teams.Add(team with { Finances = finances });
            if (penalty is not null)
            {
                penalties.Add(penalty);
            }
        }

        return new SeasonSettlement
        {
            Carset = carset with { Teams = teams },
            Penalties = penalties,
        };
    }

    private static (Finances Finances, CostCapPenalty? Penalty) Settle(
        Carset carset, SeasonResult season, Team team, IReadOnlyDictionary<string, long>? rndSpend)
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

        // Driver salaries reduce the balance but sit outside the cost cap; the rest is capped spend. A
        // career may fold R&D spend in too (M17); with none supplied this adds nothing.
        var driverSalaries = DriverSalaries(carset, season, team);
        var cappedSpend = StaffSalaries(team)
            + economy.OperatingCostPerRace * races
            + economy.CrashCostPerIncident * CrashCount(season, team)
            + (rndSpend?.GetValueOrDefault(team.Id) ?? 0);
        var expense = driverSalaries + cappedSpend;

        var penalty = CapPenalty(economy, team, cappedSpend);
        var fine = penalty?.Fine ?? 0;

        var finances = team.Finances with
        {
            Balance = team.Finances.Balance + income - expense - fine,
            PrizeMoney = prize,
            SponsorIncome = sponsorIncome,
        };
        return (finances, penalty);
    }

    // A cost-cap breach penalty when a team's capped spend overran its cost cap. Null when the team runs
    // no cap or stayed within it. The fine is a percentage of the overspend; points and the aero-test
    // restriction are recorded for later layers (M17/M18/M22) to apply.
    private static CostCapPenalty? CapPenalty(EconomyRules economy, Team team, long cappedSpend)
    {
        if (!team.Finances.HasCostCap || cappedSpend <= team.Finances.CostCap)
        {
            return null;
        }

        var overspend = cappedSpend - team.Finances.CostCap;
        var fine = overspend * economy.CostCapFinePercent / 100;
        var pointsDeducted = economy.CostCapPointsPerOverage > 0
            ? (int)(overspend / economy.CostCapPointsPerOverage)
            : 0;

        return new CostCapPenalty
        {
            TeamId = team.Id,
            Overspend = overspend,
            Fine = fine,
            PointsDeducted = pointsDeducted,
            AeroTestRestricted = true,
        };
    }

    private static long DriverSalaries(Carset carset, SeasonResult season, Team team)
    {
        long total = 0;
        foreach (var contract in carset.Contracts)
        {
            if (contract.Kind != ContractKind.Driver || contract.TeamId != team.Id)
            {
                continue;
            }

            total += contract.SalaryPerSeason
                + contract.Clauses.PerPointBonus * DriverPoints(season, contract.PartyId);
            if (season.DriversChampionId == contract.PartyId)
            {
                total += contract.Clauses.ChampionshipBonus;
            }
        }

        return total;
    }

    private static long StaffSalaries(Team team)
    {
        long total = 0;
        foreach (var member in team.Staff)
        {
            total += member.Salary;
        }

        return total;
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
