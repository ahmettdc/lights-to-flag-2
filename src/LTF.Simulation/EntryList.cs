using LTF.Domain;

namespace LTF.Simulation;

/// <summary>Builds the flat list of <see cref="Competitor"/>s that contest a carset's season.</summary>
public static class EntryList
{
    /// <summary>
    /// Pair every team's car with each of its drivers, in team-then-seat order. The carset
    /// is expected to be valid (M2 validator); an unresolved driver reference throws.
    /// </summary>
    public static IReadOnlyList<Competitor> Build(Carset carset)
    {
        var driversById = carset.Drivers.ToDictionary(d => d.Id, StringComparer.Ordinal);
        var entries = new List<Competitor>();

        foreach (var team in carset.Teams)
        {
            foreach (var driverId in team.DriverIds)
            {
                if (!driversById.TryGetValue(driverId, out var driver))
                {
                    throw new InvalidOperationException(
                        $"team '{team.Id}' references unknown driver '{driverId}'.");
                }

                entries.Add(new Competitor
                {
                    Id = driverId,
                    TeamId = team.Id,
                    Class = team.Class,
                    Driver = driver,
                    Car = team.Car,
                });
            }
        }

        return entries;
    }
}
