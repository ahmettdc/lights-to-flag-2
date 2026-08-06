using LTF.Domain.Common;
using LTF.Domain.Management;
using Xunit;

namespace LTF.Career.Tests;

public class ContractSigningTests
{
    private static ContractOffer Offer(string driverId, string teamId, long salary) => new()
    {
        DriverId = driverId,
        TeamId = teamId,
        SalaryPerSeason = salary,
        SeasonsRemaining = 3,
    };

    [Fact]
    public void A_generous_offer_signs_the_driver()
    {
        var carset = CareerFixtures.Carset();

        var result = ContractSigning.Sign(carset, Offer("d1", "alpha", 10_000_000));

        Assert.True(result.Signed);
        var contract = Assert.Single(result.Carset.Contracts);
        Assert.Equal(ContractKind.Driver, contract.Kind);
        Assert.Equal("d1", contract.PartyId);
        Assert.Equal("alpha", contract.TeamId);
        Assert.Equal(10_000_000L, contract.SalaryPerSeason);
        Assert.Equal(3, contract.SeasonsRemaining);
    }

    [Fact]
    public void A_stingy_offer_is_a_no_op()
    {
        var carset = CareerFixtures.Carset();

        var result = ContractSigning.Sign(carset, Offer("d1", "alpha", 1_000_000));

        Assert.False(result.Signed);
        Assert.Empty(result.Carset.Contracts);
        Assert.Same(carset, result.Carset); // the unchanged carset is returned as-is
    }

    [Fact]
    public void An_offer_to_an_unknown_driver_is_a_no_op()
    {
        var carset = CareerFixtures.Carset();

        var result = ContractSigning.Sign(carset, Offer("nobody", "alpha", 10_000_000));

        Assert.False(result.Signed);
        Assert.Same(carset, result.Carset);
    }

    [Fact]
    public void Signing_appends_to_existing_contracts()
    {
        var existing = new Contract
        {
            Kind = ContractKind.Driver,
            PartyId = "d3",
            TeamId = "bravo",
            SalaryPerSeason = 3_000_000,
            SeasonsRemaining = 2,
        };
        var carset = CareerFixtures.Carset() with { Contracts = [existing] };

        var result = ContractSigning.Sign(carset, Offer("d1", "alpha", 10_000_000));

        Assert.True(result.Signed);
        Assert.Equal(2, result.Carset.Contracts.Count);
    }

    [Fact]
    public void Signing_is_deterministic()
    {
        var carset = CareerFixtures.Carset();
        var offer = Offer("d1", "alpha", 10_000_000);

        var a = ContractSigning.Sign(carset, offer);
        var b = ContractSigning.Sign(carset, offer);

        Assert.Equal(a.Signed, b.Signed);
        Assert.Equal(a.ExpectedSalary, b.ExpectedSalary);
        Assert.Equal(a.Carset.Contracts.Count, b.Carset.Contracts.Count);
    }
}
