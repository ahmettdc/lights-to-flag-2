using LTF.Domain.Common;

namespace LTF.Domain.Management;

/// <summary>
/// A team's development infrastructure (M14 / ADR-0020): ten facilities, each built to a level 1–5.
/// Facilities give no car points directly — their levels set the capacity, speed, accuracy, quality
/// and risk of R&amp;D (the maths lives in the career layer, like <see cref="EconomyRules"/> feeding the
/// economy). Every facility defaults to a neutral level 3.
/// </summary>
public sealed record Facilities
{
    /// <summary>Design office — how many development projects can run at once and how error-free they are.</summary>
    public FacilityLevel DesignOffice { get; init; } = new(3);

    /// <summary>Wind tunnel — aero development efficiency and how well the tunnel correlates to the track.</summary>
    public FacilityLevel WindTunnel { get; init; } = new(3);

    /// <summary>CFD — parallel aero projects, iteration speed and confidence.</summary>
    public FacilityLevel Cfd { get; init; } = new(3);

    /// <summary>Composite manufacturing — part build slots, lead time and defect rate.</summary>
    public FacilityLevel CompositeManufacturing { get; init; } = new(3);

    /// <summary>Mechanical workshop — build quality of the mechanical package.</summary>
    public FacilityLevel MechanicalWorkshop { get; init; } = new(3);

    /// <summary>Quality control — how often a part breaks or is out of spec (feeds reliability, M5).</summary>
    public FacilityLevel QualityControl { get; init; } = new(3);

    /// <summary>Driver-in-the-loop simulator — setup/feedback quality and young-driver development.</summary>
    public FacilityLevel Simulator { get; init; } = new(3);

    /// <summary>Dyno / power-unit integration — PU development and reliability work.</summary>
    public FacilityLevel Dyno { get; init; } = new(3);

    /// <summary>Pit-crew centre — pit-stop speed and consistency (feeds M7).</summary>
    public FacilityLevel PitCrewCentre { get; init; } = new(3);

    /// <summary>Data centre — telemetry, strategy and rival analysis (feeds M5e/M24).</summary>
    public FacilityLevel DataCentre { get; init; } = new(3);

    /// <summary>A neutral mid-level facility set (all level 3), for construction without full detail.</summary>
    public static Facilities Default { get; } = new();
}
