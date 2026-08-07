namespace LTF.Domain.Racing;

/// <summary>A live pit-wall order the player can give one of their drivers during a race (M23h).</summary>
public enum RaceCommandKind
{
    /// <summary>Pit at the end of this lap (an extra stop for fresh tyres).</summary>
    BoxThisLap,

    /// <summary>Turn the engine up — faster, but harder on the car.</summary>
    PushMode,

    /// <summary>Stay out longer — push the next planned stop back.</summary>
    ExtendStint,

    /// <summary>Back off to look after the tyres — slower, easier on the car.</summary>
    ManageTyres,
}

/// <summary>
/// One recorded pit-wall order (M23h): the player told <see cref="DriverId"/> to do <see cref="Kind"/> at
/// <see cref="Lap"/> of round <see cref="Round"/>. Commands are the player's <em>recorded</em> live input — the
/// interactive race records them as they are issued and they live on <see cref="Carset.PlayerRaceCommands"/>,
/// so replaying the log reproduces the race deterministically (live = reconstruct = rollover), exactly as the
/// pre-race strategy does. Value-typed (two ints, a string and an enum), so it round-trips a save byte-stably.
/// </summary>
public sealed record RaceCommand
{
    /// <summary>The calendar round the order was given in.</summary>
    public required int Round { get; init; }

    /// <summary>The lap the order takes effect on.</summary>
    public required int Lap { get; init; }

    /// <summary>The driver the order is for (a competitor id — equal to the driver id).</summary>
    public required string DriverId { get; init; }

    /// <summary>What the driver was told to do.</summary>
    public required RaceCommandKind Kind { get; init; }
}
