using System.Collections.Generic;
using System.Linq;
using LTF.Domain;
using LTF.Domain.Management;
using Xunit;

namespace LTF.Career.Tests;

public class DriverRetirementTests
{
    // The default retirement age is 40; d1 has turned 41 and holds a live contract.
    private static Carset RetirementCarset()
    {
        var carset = CareerFixtures.Carset();
        var drivers = carset.Drivers.Select(d => d.Id == "d1" ? d with { Age = 41 } : d).ToList();
        var contracts = new List<Contract>
        {
            new()
            {
                Kind = ContractKind.Driver, PartyId = "d1", TeamId = "alpha",
                SalaryPerSeason = 1_000_000, SeasonsRemaining = 2,
            },
        };
        return carset with { Drivers = drivers, Contracts = contracts };
    }

    [Fact]
    public void A_driver_at_the_retirement_age_leaves_the_grid()
    {
        var carset = RetirementCarset();
        var seatsBefore = carset.Teams.Single(t => t.Id == "alpha").DriverIds.Count;

        var after = DriverRetirement.Retire(carset);

        Assert.DoesNotContain(after.Drivers, d => d.Id == "d1");        // removed from the field
        Assert.DoesNotContain(after.Contracts, c => c.PartyId == "d1"); // contract lapsed
        var alpha = after.Teams.Single(t => t.Id == "alpha");
        Assert.DoesNotContain("d1", alpha.DriverIds);                   // seat vacated
        Assert.Equal(seatsBefore - 1, alpha.DriverIds.Count);          // a countable vacancy
    }

    [Fact]
    public void A_grid_under_the_retirement_age_is_returned_untouched()
    {
        var carset = CareerFixtures.Carset(); // every driver is 25

        Assert.Same(carset, DriverRetirement.Retire(carset));
    }

    [Fact]
    public void Retirement_is_deterministic()
    {
        var carset = RetirementCarset();

        Assert.Equal(Ids(DriverRetirement.Retire(carset)), Ids(DriverRetirement.Retire(carset)));
    }

    private static string Ids(Carset carset) => string.Join(",", carset.Drivers.Select(d => d.Id));
}
