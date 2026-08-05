using System.Linq;
using LTF.Career;
using LTF.Domain.Common;
using LTF.Domain.Management;
using LTF.Domain.Racing;
using Xunit;

namespace LTF.Persistence.Tests;

public class CareerSaveRelationshipsTests
{
    private static CareerState Sample() => new()
    {
        Seed = 7,
        Date = new DateOnly(2025, 8, 1),
        Drivers =
        [
            new DriverCareerRecord
            {
                DriverId = "d1",
                Career = new DriverCareer { Races = 20, Wins = 3, Points = 140 },
                Morale = 38,
                Reputation = 72,
            },
            new DriverCareerRecord { DriverId = "d2", Career = DriverCareer.None },
        ],
        Teams = [new TeamHistoryRecord { TeamId = "alpha", ChampionshipsWon = 1, RaceWins = 9 }],
        Relationships =
        [
            new RelationshipRecord { AId = "d1", BId = "d2", Affinity = -36, Kind = RelationshipKind.Rival },
        ],
        Contracts =
        [
            new Contract
            {
                Kind = ContractKind.Driver,
                PartyId = "d1",
                TeamId = "alpha",
                SalaryPerSeason = 4_000_000,
                SeasonsRemaining = 2,
                Clauses = new ContractClauses { FirstDriverStatus = true, PerPointBonus = 25_000 },
            },
        ],
    };

    [Fact]
    public void A_round_trip_preserves_relationships_contracts_and_morale()
    {
        var loaded = CareerStore.Deserialize(CareerStore.Serialize(Sample()));

        var d1 = loaded.Drivers.Single(d => d.DriverId == "d1");
        Assert.Equal(38, d1.Morale);
        Assert.Equal(72, d1.Reputation);

        var bond = loaded.Relationships.Single();
        Assert.Equal("d1", bond.AId);
        Assert.Equal("d2", bond.BId);
        Assert.Equal(-36, bond.Affinity);            // the plain-int affinity survives (no struct round-trip)
        Assert.Equal(RelationshipKind.Rival, bond.Kind);

        var contract = loaded.Contracts.Single();
        Assert.Equal("d1", contract.PartyId);
        Assert.Equal(4_000_000L, contract.SalaryPerSeason);
        Assert.Equal(2, contract.SeasonsRemaining);
        Assert.True(contract.Clauses.FirstDriverStatus);
        Assert.Equal(25_000L, contract.Clauses.PerPointBonus);
    }

    [Fact]
    public void Serialization_stays_byte_stable_with_the_new_state()
    {
        var once = CareerStore.Serialize(Sample());
        var twice = CareerStore.Serialize(CareerStore.Deserialize(once));

        Assert.Equal(once, twice);
    }
}
