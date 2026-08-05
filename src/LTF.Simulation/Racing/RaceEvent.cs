namespace LTF.Simulation.Racing;

/// <summary>
/// The kind of notable thing that happened during a race. It grows as later M5 sub-steps
/// land — driver errors and collisions (M5c), neutralisations (M5d) — with M5b starting the
/// log with mechanical failures.
/// </summary>
public enum RaceEventKind
{
    MechanicalFailure,
    DriverError,
    Collision,
    StartIncident,
    VirtualSafetyCar,
    SafetyCar,
    RedFlag,
    Overtake,
    Pit,
    Penalty,
}

/// <summary>
/// One timestamped entry in a race's event log: what happened, on which lap, and to whom.
/// This is the raw material for commentary and live timing (M23) and statistics (M24); the
/// career layer also reads the participant id to evolve paddock relationships (ADR-0013).
/// </summary>
public sealed record RaceEvent
{
    public required RaceEventKind Kind { get; init; }
    public required int Lap { get; init; }
    public required string CompetitorId { get; init; }

    /// <summary>The other car involved, when there is a second party (e.g. a collision); else null.</summary>
    public string? OtherCompetitorId { get; init; }

    public required string Description { get; init; }
}
