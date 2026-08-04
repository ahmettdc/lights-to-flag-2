using LTF.Domain;

namespace LTF.Content;

/// <summary>
/// Semantic validation of a loaded <see cref="Carset"/>: cross-references, duplicates and
/// sanity checks that structural parsing (in <see cref="CarsetLoader"/>) cannot catch.
/// Returns findings rather than throwing, so a tool can report them all at once.
/// </summary>
public static class CarsetValidator
{
    public static IReadOnlyList<ValidationIssue> Validate(Carset carset)
    {
        var issues = new List<ValidationIssue>();
        void Error(string m) => issues.Add(new ValidationIssue(ValidationSeverity.Error, m));
        void Warn(string m) => issues.Add(new ValidationIssue(ValidationSeverity.Warning, m));

        // Unique ids.
        var driverIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var d in carset.Drivers)
        {
            if (!driverIds.Add(d.Id))
            {
                Error($"duplicate driver id '{d.Id}'");
            }
        }

        var circuitIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var c in carset.Circuits)
        {
            if (!circuitIds.Add(c.Id))
            {
                Error($"duplicate circuit id '{c.Id}'");
            }

            if (c.Laps <= 0)
            {
                Error($"circuit '{c.Id}' must have a positive lap count");
            }

            if (c.LapDistanceKm <= 0 || c.BaseLapTimeSeconds <= 0)
            {
                Error($"circuit '{c.Id}' must have positive lap distance and base lap time");
            }
        }

        var teamIds = new HashSet<string>(StringComparer.Ordinal);
        var seatedDrivers = new HashSet<string>(StringComparer.Ordinal);
        foreach (var t in carset.Teams)
        {
            if (!teamIds.Add(t.Id))
            {
                Error($"duplicate team id '{t.Id}'");
            }

            if (t.DriverIds.Count == 0)
            {
                Error($"team '{t.Id}' has no drivers");
            }
            else if (t.DriverIds.Count != 2)
            {
                Warn($"team '{t.Id}' has {t.DriverIds.Count} drivers (2 is typical)");
            }

            foreach (var id in t.DriverIds)
            {
                if (!driverIds.Contains(id))
                {
                    Error($"team '{t.Id}' references unknown driver '{id}'");
                }

                if (!seatedDrivers.Add(id))
                {
                    Error($"driver '{id}' is seated in more than one team");
                }
            }
        }

        foreach (var d in carset.Drivers)
        {
            if (!seatedDrivers.Contains(d.Id))
            {
                Warn($"driver '{d.Id}' is not seated in any team");
            }
        }

        // Calendar.
        var rounds = new HashSet<int>();
        foreach (var r in carset.Calendar)
        {
            if (!circuitIds.Contains(r.CircuitId))
            {
                Error($"calendar round {r.Round} references unknown circuit '{r.CircuitId}'");
            }

            if (!rounds.Add(r.Round))
            {
                Error($"duplicate calendar round number {r.Round}");
            }
        }

        for (var n = 1; n <= carset.Calendar.Count; n++)
        {
            if (!rounds.Contains(n))
            {
                Warn($"calendar is missing round {n} (rounds are not contiguous from 1)");
            }
        }

        if (carset.Rules.Points.RacePoints.Count == 0)
        {
            Error("rules.points.racePoints must list at least one score");
        }

        return issues;
    }
}
