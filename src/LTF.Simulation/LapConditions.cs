using LTF.Domain.Common;

namespace LTF.Simulation;

/// <summary>
/// The mutable state a lap is run under: tyres, fuel load and track surface. Bundled so
/// the lap-time model takes one argument and the race loop advances one object per lap.
/// </summary>
public sealed record LapConditions
{
    public required TyreState Tyre { get; init; }

    /// <summary>Fuel as a fraction of a full race load (0 … 1).</summary>
    public double FuelFraction { get; init; }

    public TrackConditions Track { get; init; } = TrackConditions.Dry;

    /// <summary>Reference conditions — fresh mediums, no fuel penalty, dry — i.e. pure pace.</summary>
    public static LapConditions Neutral { get; } = new() { Tyre = TyreState.Fresh(TyreCompound.Medium) };
}
