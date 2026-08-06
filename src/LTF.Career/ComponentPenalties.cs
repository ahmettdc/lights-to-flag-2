using LTF.Domain;
using LTF.Domain.Common;

namespace LTF.Career;

/// <summary>
/// Works out the grid penalties a season's component-allocation rules produce (M15). Each driver runs a
/// per-season pool of life-limited components (engine, gearbox, brakes); a fresh unit is fitted when the
/// current one is worn out, and once a driver has used more than the series allocation
/// (<see cref="RulesSet.ComponentAllocation"/>), every further unit costs
/// <see cref="RulesSet.GridPenaltyPerExtraComponent"/> grid places for that race.
///
/// Pure and deterministic — no RNG. Component life defaults to an even split of the season across the
/// allocation, so with no extra tuning every car exactly fits its quota and the grid is never reordered
/// (the inert default). <see cref="RulesSet.ComponentReliabilityWearInfluence"/> above zero makes a less
/// reliable car wear its components faster, overrun its allocation and differ from a reliable one — which
/// is what makes a penalty actually move the grid.
/// </summary>
public static class ComponentPenalties
{
    /// <summary>The grid places each driver loses in each round of the season, in calendar order. Every
    /// round's map is empty when no allocation is configured, or when every car exactly fits its quota.</summary>
    public static IReadOnlyList<IReadOnlyDictionary<string, int>> ForSeason(Carset carset)
    {
        var rounds = carset.Calendar.Count;
        var perRound = new List<Dictionary<string, int>>(rounds);
        for (var i = 0; i < rounds; i++)
        {
            perRound.Add(new Dictionary<string, int>(StringComparer.Ordinal));
        }

        var allocation = carset.Rules.ComponentAllocation;
        if (rounds == 0 || allocation.Count == 0)
        {
            return perRound.Select(m => (IReadOnlyDictionary<string, int>)m).ToList();
        }

        var coeff = carset.Rules.ComponentReliabilityWearInfluence;
        var lifeOverride = carset.Rules.ComponentLifeRounds;
        var penalty = carset.Rules.GridPenaltyPerExtraComponent;

        foreach (var team in carset.Teams)
        {
            // A less reliable car wears its components faster: reliability 100 keeps the full life, and
            // lower reliability shortens it in proportion to the influence coefficient (1.0 at coeff 0).
            var factor = 1.0 - (coeff * (1.0 - team.Car.Reliability.Normalized));

            foreach (var driverId in team.DriverIds)
            {
                foreach (var (kind, alloc) in allocation)
                {
                    var baseLife = lifeOverride.TryGetValue(kind, out var l) ? l : CeilDiv(rounds, Math.Max(1, alloc));
                    var life = Math.Max(1, (int)Math.Round(baseLife * factor));

                    // Walk the season fitting a fresh unit when the current one is worn out; any unit
                    // beyond the allocation drops the driver on the round it is fitted.
                    var unitsUsed = 1;
                    var lifeLeft = life;
                    for (var i = 0; i < rounds; i++)
                    {
                        if (lifeLeft == 0)
                        {
                            unitsUsed++;
                            lifeLeft = life;
                            var extra = unitsUsed - alloc;
                            if (extra > 0)
                            {
                                perRound[i][driverId] = perRound[i].GetValueOrDefault(driverId) + (extra * penalty);
                            }
                        }

                        lifeLeft--;
                    }
                }
            }
        }

        return perRound.Select(m => (IReadOnlyDictionary<string, int>)m).ToList();
    }

    private static int CeilDiv(int a, int b) => (a + b - 1) / b;
}
