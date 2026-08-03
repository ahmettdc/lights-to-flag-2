using LightsToFlag.Core.Domain;

namespace LightsToFlag.Core.Simulation;

/// <summary>Builds the simulator's competitor list from a carset's drivers and teams.</summary>
public static class EntryList
{
    /// <summary>
    /// Pair each race driver with the team its <see cref="DriverRating.TeamNumber"/>
    /// points at. Ids are stable within a carset (<c>D{index}</c>).
    /// </summary>
    public static IReadOnlyList<Competitor> BuildFromCarset(Carset carset)
    {
        var competitors = new List<Competitor>(carset.Drivers.Count);
        for (var i = 0; i < carset.Drivers.Count; i++)
        {
            var driver = carset.Drivers[i];
            var teamIndex = Math.Clamp(driver.TeamNumber - 1, 0, Math.Max(0, carset.Teams.Count - 1));
            var team = carset.Teams.Count > 0 ? carset.Teams[teamIndex] : new TeamRating { Name = "Privateer" };

            competitors.Add(new Competitor
            {
                Id = $"D{i + 1}",
                Driver = driver,
                Car = team,
                ClassIndex = Math.Max(0, team.Class - 1),
            });
        }

        return competitors;
    }
}
