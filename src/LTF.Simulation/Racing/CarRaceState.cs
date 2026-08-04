namespace LTF.Simulation.Racing;

/// <summary>
/// Mutable per-car state during a race, private to the simulator. Everything that changes
/// lap to lap — tyres, fuel, accumulated time, whether the car is still running — lives
/// here; the immutable <see cref="RaceResult"/> is built from it at the flag.
/// </summary>
internal sealed class CarRaceState
{
    public CarRaceState(Competitor competitor, int gridPosition, IRandom rng, TyreState tyre, double fuel)
    {
        Competitor = competitor;
        GridPosition = gridPosition;
        Rng = rng;
        Tyre = tyre;
        Fuel = fuel;
    }

    public Competitor Competitor { get; }
    public int GridPosition { get; }

    /// <summary>This car's independent RNG stream (forked from the race seed).</summary>
    public IRandom Rng { get; }

    public TyreState Tyre { get; set; }
    public double Fuel { get; set; }

    public double TotalTime { get; set; }
    public int LapsCompleted { get; set; }
    public double BestLap { get; set; } = double.MaxValue;

    public bool Running { get; set; } = true;
    public FinishStatus Status { get; set; } = FinishStatus.Finished;
    public string? RetirementReason { get; set; }

    public string Id => Competitor.Id;
}
