using LTF.Domain.Common;

namespace LTF.Simulation.Racing;

/// <summary>
/// One car's telemetry at the end of a lap: order and gaps, the lap just set (split into
/// sectors), and its tyre / fuel / engine-mode state. The raw material for the timing tower,
/// lap chart and telemetry screens (M23/M24). Continuous pedal / speed traces are a later,
/// representative layer and are not modelled here.
/// </summary>
public sealed record CarLapSample
{
    public required string CompetitorId { get; init; }
    public required int Position { get; init; }
    public required int Laps { get; init; }

    public required double TotalTime { get; init; }
    public required double GapToLeader { get; init; }

    /// <summary>Gap to the car directly ahead in the order (0 for the leader).</summary>
    public required double IntervalAhead { get; init; }

    /// <summary>The lap just completed and its three sector times.</summary>
    public required double LastLap { get; init; }
    public required double Sector1 { get; init; }
    public required double Sector2 { get; init; }
    public required double Sector3 { get; init; }

    public required TyreCompound TyreCompound { get; init; }
    public required int TyreAge { get; init; }
    public required double TyreWear { get; init; }
    public required double Fuel { get; init; }
    public required EngineMode EngineMode { get; init; }

    /// <summary>Battery charge as a fraction of the budget (0..1). Full in the DRS era; drawn down
    /// and regenerated across the lap in the 2026 era.</summary>
    public required double Energy { get; init; }
}

/// <summary>The running order at the end of one lap, and the race-control state during it.</summary>
public sealed record LapSnapshot
{
    public required int Lap { get; init; }
    public required IReadOnlyList<CarLapSample> Order { get; init; }

    /// <summary>Whether the lap ran green or under a neutralisation (M5d).</summary>
    public required NeutralizationState State { get; init; }
}

/// <summary>
/// Lap-by-lap record of a race — the raw material for live timing and the lap chart (M23),
/// commentary and statistics (M24). Paired with the event log on <see cref="RaceResult"/>.
/// </summary>
public sealed record RaceTelemetry
{
    public required IReadOnlyList<LapSnapshot> Laps { get; init; }
}
