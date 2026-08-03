namespace LightsToFlag.Core.Simulation;

/// <summary>The kind of notable thing that can happen on a lap (drives race commentary).</summary>
public enum RaceEventKind
{
    Start,
    Overtake,
    Pit,
    Retirement,
    FastestLap,
    SafetyCar,
    RainStarted,
    RainStopped,
    Finish,
}

/// <summary>
/// A structured race event, emitted per lap by the simulator. Kept UI-free: it carries
/// competitor ids and numbers, and the UI turns it into human commentary using the
/// driver-name map.
/// </summary>
public sealed record RaceEvent
{
    public required int Lap { get; init; }
    public required RaceEventKind Kind { get; init; }

    /// <summary>Main competitor (e.g. the overtaker, the car pitting, the retiree, the winner).</summary>
    public string? PrimaryId { get; init; }

    /// <summary>Secondary competitor (e.g. the car that was overtaken).</summary>
    public string? SecondaryId { get; init; }

    /// <summary>Position involved (e.g. the place gained in an overtake).</summary>
    public int Position { get; init; }

    /// <summary>Lap time in seconds (for a fastest-lap event).</summary>
    public double LapTimeSeconds { get; init; }

    /// <summary>Free-text detail (e.g. a retirement reason).</summary>
    public string? Note { get; init; }
}
