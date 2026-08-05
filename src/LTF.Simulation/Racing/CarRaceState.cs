using LTF.Simulation.Laps;

namespace LTF.Simulation.Racing;

/// <summary>
/// Mutable per-car state during a race, private to the simulator. Everything that changes
/// lap to lap — tyres, fuel, component health, accumulated time, whether the car is still
/// running — lives here; the immutable <see cref="RaceResult"/> is built from it at the flag.
/// </summary>
internal sealed class CarRaceState
{
    public CarRaceState(
        Competitor competitor, int gridPosition, IRandom rng, IRandom reliabilityRng, IRandom incidentRng,
        TyreState tyre, double fuel, ComponentHealth health, EngineMode mode, double topSpeed)
    {
        Competitor = competitor;
        GridPosition = gridPosition;
        Rng = rng;
        ReliabilityRng = reliabilityRng;
        IncidentRng = incidentRng;
        Tyre = tyre;
        Fuel = fuel;
        Health = health;
        Mode = mode;
        TopSpeed = topSpeed;
    }

    public Competitor Competitor { get; }
    public int GridPosition { get; }

    /// <summary>This car's independent pace RNG stream (forked from the race seed).</summary>
    public IRandom Rng { get; }

    /// <summary>
    /// A separate stream for reliability rolls, forked off the pace stream, so drawing
    /// failures never disturbs the pace draws — pace stays identical whatever the event
    /// model does.
    /// </summary>
    public IRandom ReliabilityRng { get; }

    /// <summary>Another independent stream for on-track incidents (start, driver errors,
    /// collisions), kept separate for the same reason.</summary>
    public IRandom IncidentRng { get; }

    public TyreState Tyre { get; set; }
    public double Fuel { get; set; }

    /// <summary>Per-component condition; drains each lap and gates limp mode and failures.</summary>
    public ComponentHealth Health { get; }

    /// <summary>How hard the car is being run (M5b: always <see cref="EngineMode.Standard"/>).</summary>
    public EngineMode Mode { get; set; }

    public double TotalTime { get; set; }
    public int LapsCompleted { get; set; }
    public double BestLap { get; set; } = double.MaxValue;

    /// <summary>The most recent lap time and its sector split (telemetry, M5e).</summary>
    public double LastLap { get; set; }
    public SectorTimes LastSectors { get; set; }

    /// <summary>Representative top speed (kph), fixed for this car on this circuit.</summary>
    public double TopSpeed { get; }

    /// <summary>Battery charge as a fraction of the budget (0..1). Managed only in the 2026 era
    /// (regen, Manual Override deployment, de-rating); stays full in the DRS era, where it is inert.</summary>
    public double Energy { get; set; } = 1.0;

    public bool Running { get; set; } = true;
    public FinishStatus Status { get; set; } = FinishStatus.Finished;
    public string? RetirementReason { get; set; }

    public string Id => Competitor.Id;
}
