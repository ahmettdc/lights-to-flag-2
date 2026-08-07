using LTF.Domain;
using LTF.Simulation.Racing;

namespace LTF.Career;

/// <summary>
/// Folds race results into a drivers' and a constructors' championship table (M11). Race points are
/// already awarded per the carset's points scheme by the race simulator, so this only accumulates:
/// a driver's points go to their own line and to their team's constructors' line. Deterministic —
/// every driver and team in the carset appears (even on zero), and ties break by wins then id.
/// </summary>
public static class ChampionshipStandings
{
    /// <summary>The zeroed table before a wheel is turned (M21): every driver and team present on zero
    /// points, in id order. Equivalent to <see cref="From"/> with no results — the standings screen shows
    /// this at season start, before any round has run.</summary>
    public static Standings Empty(Carset carset) => From(carset, []);

    public static Standings From(Carset carset, IReadOnlyList<RaceResult> results)
    {
        var teamOfDriver = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var team in carset.Teams)
        {
            foreach (var driverId in team.DriverIds)
            {
                teamOfDriver[driverId] = team.Id;
            }
        }

        var driverPoints = carset.Drivers.ToDictionary(d => d.Id, _ => 0, StringComparer.Ordinal);
        var driverWins = carset.Drivers.ToDictionary(d => d.Id, _ => 0, StringComparer.Ordinal);
        var driverPodiums = carset.Drivers.ToDictionary(d => d.Id, _ => 0, StringComparer.Ordinal);
        var teamPoints = carset.Teams.ToDictionary(t => t.Id, _ => 0, StringComparer.Ordinal);
        var teamWins = carset.Teams.ToDictionary(t => t.Id, _ => 0, StringComparer.Ordinal);

        foreach (var result in results)
        {
            foreach (var e in result.Classification)
            {
                var hasDriver = driverPoints.ContainsKey(e.CompetitorId);
                var hasTeam = teamOfDriver.TryGetValue(e.CompetitorId, out var teamId);

                if (hasDriver)
                {
                    driverPoints[e.CompetitorId] += e.Points;
                }

                if (hasTeam)
                {
                    teamPoints[teamId!] += e.Points;
                }

                if (e.Status != FinishStatus.Finished)
                {
                    continue;
                }

                if (e.Position == 1)
                {
                    if (hasDriver)
                    {
                        driverWins[e.CompetitorId]++;
                    }

                    if (hasTeam)
                    {
                        teamWins[teamId!]++;
                    }
                }

                if (e.Position <= 3 && hasDriver)
                {
                    driverPodiums[e.CompetitorId]++;
                }
            }
        }

        var drivers = carset.Drivers
            .OrderByDescending(d => driverPoints[d.Id])
            .ThenByDescending(d => driverWins[d.Id])
            .ThenBy(d => d.Id, StringComparer.Ordinal)
            .Select((d, i) => new DriverStanding
            {
                Position = i + 1,
                DriverId = d.Id,
                Points = driverPoints[d.Id],
                Wins = driverWins[d.Id],
                Podiums = driverPodiums[d.Id],
            })
            .ToList();

        var constructors = carset.Teams
            .OrderByDescending(t => teamPoints[t.Id])
            .ThenByDescending(t => teamWins[t.Id])
            .ThenBy(t => t.Id, StringComparer.Ordinal)
            .Select((t, i) => new ConstructorStanding
            {
                Position = i + 1,
                TeamId = t.Id,
                Points = teamPoints[t.Id],
                Wins = teamWins[t.Id],
            })
            .ToList();

        return new Standings { Drivers = drivers, Constructors = constructors };
    }
}
