namespace LTF.Simulation.Laps;

/// <summary>
/// A lap broken into three sector times (seconds). Splitting the lap is what makes live
/// timing, deltas and slipstream possible later (ROADMAP M3/M6); a whole-lap number is
/// just the <see cref="Total"/>.
/// </summary>
public readonly record struct SectorTimes(double Sector1, double Sector2, double Sector3)
{
    public double Total => Sector1 + Sector2 + Sector3;
}
