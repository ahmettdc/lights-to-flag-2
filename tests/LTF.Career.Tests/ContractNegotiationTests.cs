using System.Collections.Generic;
using System.Linq;
using LTF.Domain;
using LTF.Domain.Racing;
using Xunit;

namespace LTF.Career.Tests;

public class ContractNegotiationTests
{
    [Fact]
    public void A_generous_offer_is_accepted()
    {
        var carset = CareerFixtures.Carset();

        Assert.True(ContractNegotiation.Evaluate(carset, "d1", 10_000_000).Accepted);
    }

    [Fact]
    public void A_stingy_offer_is_rejected()
    {
        var carset = CareerFixtures.Carset();

        Assert.False(ContractNegotiation.Evaluate(carset, "d1", 1_000_000).Accepted);
    }

    [Fact]
    public void A_good_teammate_bond_lowers_the_asking_price()
    {
        var carset = CareerFixtures.Carset();
        var ally = carset with { Relationships = RelationshipGraph.Empty.Shifted("d1", "d2", 50) };
        var feud = carset with { Relationships = RelationshipGraph.Empty.Shifted("d1", "d2", -50) };

        // The same offer the happy driver accepts, the feuding one holds out on.
        Assert.True(ContractNegotiation.Evaluate(ally, "d1", 5_000_000).Accepted);
        Assert.False(ContractNegotiation.Evaluate(feud, "d1", 5_000_000).Accepted);
    }

    [Fact]
    public void A_loyal_driver_re_signs_for_less()
    {
        var carset = CareerFixtures.Carset();
        var loyal = carset with
        {
            Drivers = WithDriver(carset, "d1", d => d with { Personality = Personality.Neutral with { Loyalty = new(100) } }),
        };

        Assert.True(ContractNegotiation.Evaluate(loyal, "d1", 4_500_000).Accepted);   // loyalty softens the demand
        Assert.False(ContractNegotiation.Evaluate(carset, "d1", 4_500_000).Accepted); // a neutral driver holds out
    }

    [Fact]
    public void A_bigger_name_demands_more()
    {
        var carset = CareerFixtures.Carset();
        var star = carset with { Drivers = WithDriver(carset, "d1", d => d with { Reputation = new(100) }) };

        Assert.False(ContractNegotiation.Evaluate(star, "d1", 6_000_000).Accepted);  // the star holds out
        Assert.True(ContractNegotiation.Evaluate(carset, "d1", 6_000_000).Accepted); // the journeyman signs
    }

    [Fact]
    public void Negotiation_is_deterministic()
    {
        var carset = CareerFixtures.Carset() with
        {
            Relationships = RelationshipGraph.Empty.Shifted("d1", "d2", 30),
        };

        var a = ContractNegotiation.Evaluate(carset, "d1", 5_000_000);
        var b = ContractNegotiation.Evaluate(carset, "d1", 5_000_000);

        Assert.Equal(a.ExpectedSalary, b.ExpectedSalary);
        Assert.Equal(a.Outcome, b.Outcome);
    }

    private static IReadOnlyList<Driver> WithDriver(Carset carset, string id, System.Func<Driver, Driver> edit) =>
        carset.Drivers.Select(d => d.Id == id ? edit(d) : d).ToList();
}
