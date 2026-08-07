using System.Linq;
using LTF.Career;
using LTF.Domain;
using LTF.Domain.Racing;
using LTF.Domain.Rnd;
using Xunit;

namespace LTF.Persistence.Tests;

/// <summary>
/// FIA development-freeze persistence (Ri4): the voted-in freeze regime lives on the carset's regulations, not
/// the shipped carset, so it is captured and restored through a save. Empty by default, so a freeze-free career
/// is byte-identical. Value-typed (CarAxis + mode enums) → round-trips byte-stably.
/// </summary>
public class CareerSaveRegulationTests
{
    private static CareerState WithFreezes() => new()
    {
        Seed = 14,
        Date = new DateOnly(2025, 12, 3),
        DevelopmentFreezes =
        [
            new AxisFreeze { Axis = CarAxis.PowerUnit, Mode = DevelopmentFreezeMode.InSeasonOnly },
            new AxisFreeze { Axis = CarAxis.MechanicalGrip, Mode = DevelopmentFreezeMode.Full },
        ],
    };

    private static Carset FrozenCarset() => new()
    {
        Id = "mini",
        Name = "Mini",
        Rules = new RulesSet { SeriesName = "S", Points = new PointsScheme { RacePoints = [25, 18] } },
        Circuits = [],
        Drivers = [],
        Calendar = [],
        Teams = [],
        Regulations = new RegulationSet
        {
            DevelopmentFreezes = [new AxisFreeze { Axis = CarAxis.PowerUnit, Mode = DevelopmentFreezeMode.Full }],
        },
    };

    [Fact]
    public void A_round_trip_preserves_the_development_freezes()
    {
        var loaded = CareerStore.Deserialize(CareerStore.Serialize(WithFreezes()));

        Assert.Equal(2, loaded.DevelopmentFreezes.Count);
        var pu = loaded.DevelopmentFreezes.Single(f => f.Axis == CarAxis.PowerUnit);
        Assert.Equal(DevelopmentFreezeMode.InSeasonOnly, pu.Mode);
        var mech = loaded.DevelopmentFreezes.Single(f => f.Axis == CarAxis.MechanicalGrip);
        Assert.Equal(DevelopmentFreezeMode.Full, mech.Mode);
    }

    [Fact]
    public void Serialization_stays_byte_stable_with_freezes()
    {
        var once = CareerStore.Serialize(WithFreezes());
        var twice = CareerStore.Serialize(CareerStore.Deserialize(once));
        Assert.Equal(once, twice);
    }

    [Fact]
    public void Capture_and_restore_carry_the_freezes_through_a_save()
    {
        var state = CareerState.Capture(FrozenCarset(), new DateOnly(2025, 1, 1), 7);
        Assert.Single(state.DevelopmentFreezes);

        // Restore onto a freshly loaded (freeze-free) carset — the save is authoritative for the freeze.
        var restored = state.RestoreInto(FrozenCarset() with { Regulations = RegulationSet.Drs });
        Assert.Single(restored.Regulations.DevelopmentFreezes);
        Assert.Equal(CarAxis.PowerUnit, restored.Regulations.DevelopmentFreezes[0].Axis);
        Assert.Equal(DevelopmentFreezeMode.Full, restored.Regulations.DevelopmentFreezes[0].Mode);
    }

    [Fact]
    public void A_carset_without_a_freeze_captures_none()
    {
        var carset = FrozenCarset() with { Regulations = RegulationSet.Drs };
        Assert.Empty(CareerState.Capture(carset, new DateOnly(2025, 1, 1), 7).DevelopmentFreezes);
    }
}
