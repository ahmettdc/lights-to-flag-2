using LTF.Domain;
using LTF.Domain.Management;
using LTF.Domain.Racing;

namespace LTF.Career;

/// <summary>
/// Settles a team's finances at season rollover (M13). This first step books the income side — prize
/// money by constructors'-championship position, flat TV income, and sponsor fees and bonuses — and
/// credits it to the balance; the expense side folds in with the next sub-step. Pure, deterministic
/// arithmetic over the finished <see cref="SeasonResult"/> (no RNG), so the same carset and season
/// always reach the same books. With an empty economy no money moves and the carset is untouched.
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

        return team.Finances with
        {
            Balance = team.Finances.Balance + income,
            PrizeMoney = prize,
            SponsorIncome = sponsorIncome,
        };
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
