using System.Linq;
using LTF.Career;
using LTF.Domain;
using LTF.Domain.Racing;
using Xunit;

namespace LTF.Persistence.Tests;

/// <summary>
/// Player live-command persistence (M23h): the player's recorded pit-wall orders are a mutation, not shipped
/// content, so they are captured and restored through a save. Empty by default → a career never driven live is
/// byte-identical. Value-typed (two ints + string + enum) → round-trips byte-stably.
/// </summary>
public class CareerSaveCommandTests
{
    private static CareerState WithCommands() => new()
    {
        Seed = 22,
        Date = new DateOnly(2025, 7, 1),
        PlayerRaceCommands =
        [
            new RaceCommand { Round = 1, Lap = 5, DriverId = "d1", Kind = RaceCommandKind.BoxThisLap },
            new RaceCommand { Round = 1, Lap = 12, DriverId = "d1", Kind = RaceCommandKind.PushMode },
            new RaceCommand { Round = 3, Lap = 8, DriverId = "d2", Kind = RaceCommandKind.ExtendStint },
        ],
    };

    private static Carset CommandCarset() => new()
    {
        Id = "mini",
        Name = "Mini",
        Rules = new RulesSet { SeriesName = "S", Points = new PointsScheme { RacePoints = [25, 18] } },
        Circuits = [],
        Drivers = [],
        Calendar = [],
        Teams = [],
        PlayerRaceCommands = [new RaceCommand { Round = 2, Lap = 4, DriverId = "d3", Kind = RaceCommandKind.ManageTyres }],
    };

    [Fact]
    public void A_round_trip_preserves_the_commands()
    {
        var loaded = CareerStore.Deserialize(CareerStore.Serialize(WithCommands()));

        Assert.Equal(3, loaded.PlayerRaceCommands.Count);
        var box = loaded.PlayerRaceCommands.Single(c => c.Kind == RaceCommandKind.BoxThisLap);
        Assert.Equal(1, box.Round);
        Assert.Equal(5, box.Lap);
        Assert.Equal("d1", box.DriverId);
    }

    [Fact]
    public void Serialization_stays_byte_stable_with_commands()
    {
        var once = CareerStore.Serialize(WithCommands());
        var twice = CareerStore.Serialize(CareerStore.Deserialize(once));
        Assert.Equal(once, twice);
    }

    [Fact]
    public void Capture_and_restore_carry_the_commands_through_a_save()
    {
        var state = CareerState.Capture(CommandCarset(), new DateOnly(2025, 1, 1), 7);
        Assert.Single(state.PlayerRaceCommands);

        var restored = state.RestoreInto(CommandCarset() with { PlayerRaceCommands = [] });
        Assert.Single(restored.PlayerRaceCommands);
        Assert.Equal(RaceCommandKind.ManageTyres, restored.PlayerRaceCommands[0].Kind);
    }

    [Fact]
    public void A_carset_never_driven_live_captures_no_commands()
    {
        var carset = CommandCarset() with { PlayerRaceCommands = [] };
        Assert.Empty(CareerState.Capture(carset, new DateOnly(2025, 1, 1), 7).PlayerRaceCommands);
    }
}
