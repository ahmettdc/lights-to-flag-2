using LTF.Domain;

namespace LTF.Career;

/// <summary>
/// Retires drivers who have reached the mandatory retirement age at season rollover (M18 / ADR-0015): a
/// driver at or over <see cref="LTF.Domain.Racing.RulesSet.RetirementAge"/> leaves the grid — removed from
/// the carset's drivers, their contract lapsed, and their seat vacated on their team, leaving a countable
/// vacancy for the transfer market to fill. Pure and deterministic (a plain age threshold, no RNG); with no
/// over-age driver the carset is returned untouched, so retirement is inert until drivers actually age.
/// </summary>
public static class DriverRetirement
{
    public static Carset Retire(Carset carset)
    {
        var retirementAge = carset.Rules.RetirementAge;
        var retiring = new HashSet<string>(StringComparer.Ordinal);
        foreach (var driver in carset.Drivers)
        {
            if (driver.Age >= retirementAge)
            {
                retiring.Add(driver.Id);
            }
        }

        if (retiring.Count == 0)
        {
            return carset;
        }

        var drivers = carset.Drivers.Where(d => !retiring.Contains(d.Id)).ToList();
        var contracts = carset.Contracts.Where(c => !retiring.Contains(c.PartyId)).ToList();
        var teams = carset.Teams
            .Select(t => retiring.Overlaps(t.DriverIds)
                ? t with { DriverIds = t.DriverIds.Where(id => !retiring.Contains(id)).ToList() }
                : t)
            .ToList();

        return carset with { Drivers = drivers, Contracts = contracts, Teams = teams };
    }
}
