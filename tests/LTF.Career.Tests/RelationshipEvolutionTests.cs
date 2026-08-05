using System.Linq;
using LTF.Domain;
using LTF.Domain.Racing;
using LTF.Simulation.Racing;
using Xunit;

namespace LTF.Career.Tests;

public class RelationshipEvolutionTests
{
    private static RaceEvent Collision(string a, string b) => new()
    {
        Kind = RaceEventKind.Collision,
        Lap = 5,
        CompetitorId = a,
        OtherCompetitorId = b,
        Description = "contact",
    };

    private static SeasonResult SeasonWith(Carset carset, params RaceEvent[] events)
    {
        var race = CareerFixtures.Race(("d1", 25), ("d2", 18), ("d3", 15), ("d4", 12))
            with { Events = events };
        var rounds = new[] { race };
        return new SeasonResult
        {
            Standings = ChampionshipStandings.From(carset, rounds),
            Rounds = rounds,
            PoleSitters = [],
        };
    }

    private static int MoraleOf(Carset carset, string id) =>
        carset.Drivers.Single(d => d.Id == id).Morale.Value;

    [Fact]
    public void A_teammate_collision_sours_their_bond_and_morale()
    {
        var carset = CareerFixtures.Carset(); // alpha d1/d2, bravo d3/d4

        var after = RelationshipEvolution.Apply(carset, SeasonWith(carset, Collision("d1", "d2")));

        var bond = after.Relationships.Between("d1", "d2");
        Assert.NotNull(bond);
        Assert.True(bond!.Affinity.Value < 0);        // soured
        Assert.True(MoraleOf(after, "d1") < 50);       // both unhappy
        Assert.True(MoraleOf(after, "d2") < 50);
    }

    [Fact]
    public void A_collision_between_rivals_leaves_the_graph_untouched()
    {
        var carset = CareerFixtures.Carset();

        var after = RelationshipEvolution.Apply(carset, SeasonWith(carset, Collision("d1", "d3")));

        Assert.Empty(after.Relationships.Relationships); // different teams — no bond in M12
        Assert.Equal(50, MoraleOf(after, "d1"));
    }

    [Fact]
    public void A_race_without_collisions_is_inert()
    {
        var carset = CareerFixtures.Carset();

        var after = RelationshipEvolution.Apply(carset, SeasonWith(carset));

        Assert.Empty(after.Relationships.Relationships);
        Assert.Equal(50, MoraleOf(after, "d1"));
    }

    [Fact]
    public void A_higher_ego_pair_feuds_harder()
    {
        var calm = CareerFixtures.Carset();
        var fiery = calm with
        {
            Drivers = calm.Drivers
                .Select(d => d.Id is "d1" or "d2" ? d with { Personality = HighEgo() } : d)
                .ToList(),
        };

        var calmAfter = RelationshipEvolution.Apply(calm, SeasonWith(calm, Collision("d1", "d2")));
        var fieryAfter = RelationshipEvolution.Apply(fiery, SeasonWith(fiery, Collision("d1", "d2")));

        Assert.True(fieryAfter.Relationships.Between("d1", "d2")!.Affinity.Value
            < calmAfter.Relationships.Between("d1", "d2")!.Affinity.Value);
    }

    [Fact]
    public void Repeated_teammate_contact_compounds()
    {
        var carset = CareerFixtures.Carset();

        var once = RelationshipEvolution.Apply(carset, SeasonWith(carset, Collision("d1", "d2")));
        var twice = RelationshipEvolution.Apply(carset, SeasonWith(carset, Collision("d1", "d2"), Collision("d1", "d2")));

        Assert.True(twice.Relationships.Between("d1", "d2")!.Affinity.Value
            < once.Relationships.Between("d1", "d2")!.Affinity.Value);
    }

    [Fact]
    public void Evolution_is_deterministic()
    {
        var carset = CareerFixtures.Carset();
        var season = SeasonWith(carset, Collision("d1", "d2"));

        Assert.Equal(Key(RelationshipEvolution.Apply(carset, season)), Key(RelationshipEvolution.Apply(carset, season)));
    }

    private static Personality HighEgo() => Personality.Neutral with { Ego = new(100) };

    private static string Key(Carset carset) =>
        string.Join(";", carset.Relationships.Relationships.Select(r => $"{r.AId}-{r.BId}:{r.Affinity.Value}")) + "|" +
        string.Join(";", carset.Drivers.Select(d => $"{d.Id}:{d.Morale.Value}"));
}
