using LTF.Domain;
using LTF.Domain.Racing;
using LTF.Simulation.Racing;

namespace LTF.Career;

/// <summary>
/// Rolls a finished season's results into persistent career records (M11d). Every driver's
/// <see cref="DriverCareer"/> tally — races, wins, podiums, poles, fastest laps, championships and
/// points — grows by what they did this season, and every team's championship and win history grows
/// with it. Returns a fresh carset with the advanced records, so the same season can be replayed and
/// several seasons chained by feeding the result back in. Pure and deterministic, as the Career layer
/// must be: the same season applied to the same carset always yields the same records.
/// </summary>
public static class CareerRollover
{
    /// <summary>Return a copy of the carset with every driver's and team's history advanced by one
    /// season's results.</summary>
    public static Carset Apply(Carset carset, SeasonResult season)
    {
        var races = new Dictionary<string, int>(StringComparer.Ordinal);
        var wins = new Dictionary<string, int>(StringComparer.Ordinal);
        var podiums = new Dictionary<string, int>(StringComparer.Ordinal);
        var poles = new Dictionary<string, int>(StringComparer.Ordinal);
        var fastestLaps = new Dictionary<string, int>(StringComparer.Ordinal);
        var points = new Dictionary<string, double>(StringComparer.Ordinal);

        foreach (var round in season.Rounds)
        {
            foreach (var e in round.Classification)
            {
                // Everyone classified took the start, so it counts as a race entered.
                races[e.CompetitorId] = races.GetValueOrDefault(e.CompetitorId) + 1;
                points[e.CompetitorId] = points.GetValueOrDefault(e.CompetitorId) + e.Points;

                if (e.Status != FinishStatus.Finished)
                {
                    continue;
                }

                if (e.Position == 1)
                {
                    wins[e.CompetitorId] = wins.GetValueOrDefault(e.CompetitorId) + 1;
                }

                if (e.Position <= 3)
                {
                    podiums[e.CompetitorId] = podiums.GetValueOrDefault(e.CompetitorId) + 1;
                }
            }

            if (round.FastestLapCompetitorId is { } fastestId)
            {
                fastestLaps[fastestId] = fastestLaps.GetValueOrDefault(fastestId) + 1;
            }
        }

        foreach (var poleId in season.PoleSitters)
        {
            if (poleId is { } id)
            {
                poles[id] = poles.GetValueOrDefault(id) + 1;
            }
        }

        var driversChampion = season.DriversChampionId;
        var constructorsChampion = season.ConstructorsChampionId;

        var drivers = carset.Drivers
            .Select(d => d with
            {
                Career = new DriverCareer
                {
                    Races = d.Career.Races + races.GetValueOrDefault(d.Id),
                    Wins = d.Career.Wins + wins.GetValueOrDefault(d.Id),
                    Podiums = d.Career.Podiums + podiums.GetValueOrDefault(d.Id),
                    Poles = d.Career.Poles + poles.GetValueOrDefault(d.Id),
                    FastestLaps = d.Career.FastestLaps + fastestLaps.GetValueOrDefault(d.Id),
                    Championships = d.Career.Championships + (d.Id == driversChampion ? 1 : 0),
                    Points = d.Career.Points + points.GetValueOrDefault(d.Id),
                },
            })
            .ToList();

        var teams = carset.Teams
            .Select(t => t with
            {
                ChampionshipsWon = t.ChampionshipsWon + (t.Id == constructorsChampion ? 1 : 0),
                RaceWins = t.RaceWins + t.DriverIds.Sum(id => wins.GetValueOrDefault(id)),
            })
            .ToList();

        return carset with { Drivers = drivers, Teams = teams };
    }
}
