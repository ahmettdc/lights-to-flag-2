using LTF.Domain.Common;

namespace LTF.Domain.Management;

/// <summary>
/// One life-limited component (engine, gearbox, brakes) and how much of its season
/// allocation the car has used. Going over the allocation triggers a grid penalty per
/// the <see cref="Racing.RulesSet"/>; tracked by the component-management layer (M15).
/// </summary>
public sealed record PowerUnitComponent
{
    public required ComponentKind Kind { get; init; }

    /// <summary>Reliability of a fresh example of this component.</summary>
    public required Rating Reliability { get; init; }

    /// <summary>Units used so far this season.</summary>
    public int UnitsUsed { get; init; }

    /// <summary>Life consumed on the current unit, 0.0 (fresh) to 1.0 (worn out).</summary>
    public double CurrentWear { get; init; }
}
