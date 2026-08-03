using LTF.Domain.Common;

namespace LTF.Domain.Racing;

/// <summary>
/// A team's car performance, on the shared 1–100 <see cref="Rating"/> scale. These are
/// the levers the R&D layer (M14) moves and the simulation (Phase 1) reads.
/// </summary>
public sealed record Car
{
    /// <summary>Aerodynamic efficiency / downforce.</summary>
    public required Rating Aerodynamics { get; init; }

    /// <summary>Mechanical grip from chassis and suspension.</summary>
    public required Rating Chassis { get; init; }

    /// <summary>Power-unit output.</summary>
    public required Rating PowerUnit { get; init; }

    /// <summary>How gently the car uses its tyres.</summary>
    public required Rating TyreGentleness { get; init; }

    /// <summary>Baseline reliability (per-component detail lives in the power-unit pool).</summary>
    public required Rating Reliability { get; init; }

    /// <summary>Supplier of the power unit (may differ from the constructor).</summary>
    public string EngineSupplier { get; init; } = "";

    /// <summary>
    /// Rough overall car level, pace-weighted. Convenience for standings previews and
    /// tests; the simulation blends the individual ratings per circuit rather than this.
    /// </summary>
    public int Overall => (int)Math.Round(
        (Aerodynamics.Value * 0.34)
        + (PowerUnit.Value * 0.30)
        + (Chassis.Value * 0.24)
        + (TyreGentleness.Value * 0.06)
        + (Reliability.Value * 0.06));
}
