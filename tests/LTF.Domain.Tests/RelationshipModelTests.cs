using LTF.Domain;
using LTF.Domain.Common;
using LTF.Domain.Racing;
using Xunit;

namespace LTF.Domain.Tests;

public class RelationshipModelTests
{
    // --- Affinity (signed, −100…+100) ---

    [Fact]
    public void Affinity_clamps_into_range()
    {
        Assert.Equal(Affinity.Max, Affinity.Clamped(500).Value);
        Assert.Equal(Affinity.Min, Affinity.Clamped(-500).Value);
        Assert.Equal(0, Affinity.Neutral.Value);
        Assert.Equal(0, new Affinity().Value); // struct default is neutral
    }

    [Fact]
    public void Affinity_shifted_moves_and_clamps()
    {
        Assert.Equal(30, new Affinity(10).Shifted(20).Value);
        Assert.Equal(Affinity.Max, new Affinity(90).Shifted(50).Value);   // clamps at +100
        Assert.Equal(Affinity.Min, new Affinity(-90).Shifted(-50).Value); // clamps at −100
    }

    [Fact]
    public void Affinity_rejects_out_of_range()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Affinity(101));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Affinity(-101));
    }

    // --- Personality ---

    [Fact]
    public void A_neutral_personality_is_all_fifty()
    {
        var p = Personality.Neutral;

        Assert.Equal(50, p.Ego.Value);
        Assert.Equal(50, p.Loyalty.Value);
        Assert.Equal(50, p.Temperament.Value);
        Assert.Equal(50, p.Ambition.Value);
    }

    [Fact]
    public void A_driver_defaults_to_a_neutral_personality()
    {
        var driver = new Driver
        {
            Id = "d1", FirstName = "A", LastName = "B", Age = 25, Attributes = Attributes(),
        };

        Assert.Equal(Personality.Neutral, driver.Personality);
    }

    // --- RelationshipGraph ---

    [Fact]
    public void A_bond_reads_the_same_regardless_of_id_order()
    {
        var graph = RelationshipGraph.Empty.Shifted("d2", "d1", 15);

        Assert.Equal(15, graph.Between("d1", "d2")!.Affinity.Value);
        Assert.Equal(15, graph.Between("d2", "d1")!.Affinity.Value); // order-agnostic
        Assert.Single(graph.Relationships);                          // one canonical bond, not two
    }

    [Fact]
    public void Shifting_creates_then_moves_a_single_bond()
    {
        var graph = RelationshipGraph.Empty
            .Shifted("a", "b", 40)
            .Shifted("b", "a", -100); // same pair, reversed order

        Assert.Single(graph.Relationships);
        Assert.Equal(-60, graph.Between("a", "b")!.Affinity.Value); // 40 − 100
    }

    [Fact]
    public void An_unknown_bond_is_null_and_the_empty_graph_is_the_carset_default()
    {
        Assert.Null(RelationshipGraph.Empty.Between("x", "y"));
        Assert.Empty(RelationshipGraph.Empty.Relationships);

        Assert.Same(RelationshipGraph.Empty, MiniCarset().Relationships);
    }

    private static DriverAttributes Attributes() => new()
    {
        Pace = new(70), Racecraft = new(70), Consistency = new(70),
        TyreManagement = new(70), WetWeather = new(70), Feedback = new(70),
    };

    private static Carset MiniCarset() => new()
    {
        Id = "c",
        Name = "C",
        Rules = new RulesSet { SeriesName = "S", Points = new PointsScheme { RacePoints = [25] } },
        Teams = [],
        Drivers = [],
        Circuits = [],
        Calendar = [],
    };
}
