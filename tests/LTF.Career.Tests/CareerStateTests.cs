using System.Linq;
using LTF.Domain;
using LTF.Domain.Common;
using LTF.Domain.Management;
using LTF.Domain.Racing;
using Xunit;

namespace LTF.Career.Tests;

public class CareerStateTests
{
    [Fact]
    public void Capture_then_restore_carries_relationships_contracts_and_morale()
    {
        var baseCarset = CareerFixtures.Carset();
        var carset = baseCarset with
        {
            Drivers = baseCarset.Drivers.Select(d => d.Id == "d1" ? d with { Morale = new(38) } : d).ToList(),
            Relationships = RelationshipGraph.Empty.Shifted("d1", "d2", -36),
            Contracts =
            [
                new Contract { Kind = ContractKind.Driver, PartyId = "d1", TeamId = "alpha", SalaryPerSeason = 1_000_000, SeasonsRemaining = 2 },
            ],
        };

        var state = CareerState.Capture(carset, new DateOnly(2025, 9, 1), 3);
        var restored = state.RestoreInto(Reset(carset));

        Assert.Equal(-36, restored.Relationships.Between("d1", "d2")!.Affinity.Value);
        Assert.Equal(38, restored.Drivers.Single(d => d.Id == "d1").Morale.Value);
        Assert.Single(restored.Contracts);
        Assert.Equal("d1", restored.Contracts[0].PartyId);
    }

    [Fact]
    public void Restoring_an_empty_snapshot_clears_the_graph_and_contracts()
    {
        var carset = CareerFixtures.Carset() with
        {
            Relationships = RelationshipGraph.Empty.Shifted("d1", "d2", 20),
            Contracts =
            [
                new Contract { Kind = ContractKind.Driver, PartyId = "d1", TeamId = "alpha", SalaryPerSeason = 1, SeasonsRemaining = 1 },
            ],
        };

        // A snapshot captured from a graph-free, contract-free carset.
        var empty = CareerState.Capture(CareerFixtures.Carset(), new DateOnly(2025, 1, 1), 0);

        var restored = empty.RestoreInto(carset);

        Assert.Empty(restored.Relationships.Relationships); // restored wholesale from the (empty) snapshot
        Assert.Empty(restored.Contracts);
    }

    // Reset the mutable state so a successful restore is unambiguous.
    private static Carset Reset(Carset carset) => carset with
    {
        Drivers = carset.Drivers.Select(d => d with { Morale = new(50) }).ToList(),
        Relationships = RelationshipGraph.Empty,
        Contracts = [],
    };
}
