using LTF.Domain.Common;

namespace LTF.Domain.Management;

/// <summary>
/// The six independent pressures a team principal lives under (ADR-0025), each on the 0–100
/// <see cref="Pressure"/> scale and moved by the board review as a season plays out (M17). They are
/// independent: a team can be winning on track yet under financial pressure. <see cref="BoardConfidence"/>
/// rises with the board's faith in the principal (higher is safer); the five pressures rise as things go
/// wrong (higher is worse).
/// </summary>
public sealed record PressureMetrics
{
    /// <summary>The board's confidence in the principal — higher is safer; a collapse risks the sack.</summary>
    public Pressure BoardConfidence { get; init; }

    /// <summary>Pressure from on-track results.</summary>
    public Pressure SportingPressure { get; init; }

    /// <summary>Pressure from the team's finances.</summary>
    public Pressure FinancialPressure { get; init; }

    /// <summary>Pressure from sponsors' expectations.</summary>
    public Pressure SponsorPressure { get; init; }

    /// <summary>Pressure from the media narrative.</summary>
    public Pressure MediaPressure { get; init; }

    /// <summary>Pressure from within the team (staff, drivers).</summary>
    public Pressure InternalPressure { get; init; }

    /// <summary>A balanced midpoint (all 50) — the default before a board sets real starting values.</summary>
    public static PressureMetrics Neutral { get; } = new()
    {
        BoardConfidence = new(50),
        SportingPressure = new(50),
        FinancialPressure = new(50),
        SponsorPressure = new(50),
        MediaPressure = new(50),
        InternalPressure = new(50),
    };
}
