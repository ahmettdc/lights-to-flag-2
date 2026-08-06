using System.Collections.Generic;
using System.Linq;
using LTF.Domain;
using LTF.Domain.Common;
using LTF.Domain.Racing;
using Xunit;

namespace LTF.Career.Tests;

public class ComponentPenaltiesTests
{
    // alpha (car 85, reliability 85) and bravo (car 70, reliability 70) over eight rounds.
    private static Carset WithComponents(double coeff, int engineAllocation)
    {
        var carset = CareerFixtures.SeasonCarset(rounds: 8);
        return carset with
        {
            Rules = carset.Rules with
            {
                ComponentAllocation = new Dictionary<ComponentKind, int> { [ComponentKind.Engine] = engineAllocation },
                ComponentReliabilityWearInfluence = coeff,
                GridPenaltyPerExtraComponent = 5,
            },
        };
    }

    [Fact]
    public void A_carset_without_allocation_has_no_penalties()
    {
        var penalties = ComponentPenalties.ForSeason(CareerFixtures.SeasonCarset(rounds: 8));

        Assert.Equal(8, penalties.Count);
        Assert.All(penalties, p => Assert.Empty(p));
    }

    [Fact]
    public void Exactly_fitting_the_quota_incurs_no_penalty()
    {
        // Coefficient 0 → every car wears at the same rate → the even-split life exactly fits the quota.
        var penalties = ComponentPenalties.ForSeason(WithComponents(coeff: 0.0, engineAllocation: 2));

        Assert.All(penalties, p => Assert.Empty(p));
    }

    [Fact]
    public void An_unreliable_car_overruns_its_quota_and_is_penalised()
    {
        // Engine allocation 2 over 8 rounds → even-split life 4. The reliable car (85) keeps a life of 4
        // and fits two engines; the unreliable car (70) wears to a life of 3, needs a third engine at
        // round index 6, and drops five places there.
        var penalties = ComponentPenalties.ForSeason(WithComponents(coeff: 0.5, engineAllocation: 2));

        Assert.Equal(5, penalties[6]["d3"]);   // bravo's drivers
        Assert.Equal(5, penalties[6]["d4"]);
        Assert.False(penalties[6].ContainsKey("d1")); // alpha's reliable car fits its quota
        Assert.False(penalties[6].ContainsKey("d2"));

        for (var i = 0; i < penalties.Count; i++)
        {
            if (i != 6)
            {
                Assert.Empty(penalties[i]);
            }
        }
    }

    [Fact]
    public void The_penalty_schedule_is_deterministic()
    {
        var carset = WithComponents(coeff: 0.5, engineAllocation: 2);

        Assert.Equal(Key(ComponentPenalties.ForSeason(carset)), Key(ComponentPenalties.ForSeason(carset)));
    }

    private static string Key(IReadOnlyList<IReadOnlyDictionary<string, int>> penalties) =>
        string.Join("|", penalties.Select(p =>
            string.Join(",", p.OrderBy(kv => kv.Key, System.StringComparer.Ordinal).Select(kv => $"{kv.Key}:{kv.Value}"))));
}
