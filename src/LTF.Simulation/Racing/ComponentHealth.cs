using LTF.Domain.Common;

namespace LTF.Simulation.Racing;

/// <summary>
/// Per-component condition of one car during a race, on a 1.0 (fresh) → 0.0 (worn out)
/// scale, one entry per <see cref="ComponentKind"/>. Health bleeds off each lap; once low it
/// both slows the car (limp) and raises the chance of a terminal failure. Kept mutable and
/// private to the simulator — it changes lap to lap and never leaves the race loop.
/// </summary>
internal sealed class ComponentHealth
{
    private static readonly int Count = Enum.GetValues<ComponentKind>().Length;

    private readonly double[] _health;

    private ComponentHealth(double[] health) => _health = health;

    /// <summary>A brand-new set of components, all at full health.</summary>
    public static ComponentHealth Fresh()
    {
        var health = new double[Count];
        Array.Fill(health, 1.0);
        return new ComponentHealth(health);
    }

    /// <summary>Current health of one component, 0.0–1.0.</summary>
    public double Of(ComponentKind kind) => _health[(int)kind];

    /// <summary>The weakest component's health — what limp mode and failure risk key off.</summary>
    public double Lowest => _health.Min();

    /// <summary>Wear a component by <paramref name="amount"/>, clamped at 0.</summary>
    public void Degrade(ComponentKind kind, double amount) =>
        _health[(int)kind] = Math.Max(0.0, _health[(int)kind] - amount);
}
