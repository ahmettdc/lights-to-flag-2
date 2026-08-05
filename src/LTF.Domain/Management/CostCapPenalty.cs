namespace LTF.Domain.Management;

/// <summary>
/// The outcome of a team breaching the series' cost cap in a season (M13 / ADR-0010): capped spending
/// (staff, operating and crash costs — driver salaries sit outside the cap) exceeded <see
/// cref="Finances.CostCap"/>. The economy layer produces the correct penalty as data; the sporting
/// consequences (<see cref="PointsDeducted"/>, <see cref="AeroTestRestricted"/>) are surfaced by later
/// layers (M17/M18/M22). The <see cref="Fine"/> is already deducted from the team's balance at settlement.
/// </summary>
public sealed record CostCapPenalty
{
    /// <summary>The team that breached the cap.</summary>
    public required string TeamId { get; init; }

    /// <summary>How far capped spending exceeded the cost cap.</summary>
    public long Overspend { get; init; }

    /// <summary>Financial penalty, already deducted from the team's balance.</summary>
    public long Fine { get; init; }

    /// <summary>Constructor points to be deducted (applied by the standings/season layer later).</summary>
    public int PointsDeducted { get; init; }

    /// <summary>Whether the team's aerodynamic testing is restricted as a consequence of the breach.</summary>
    public bool AeroTestRestricted { get; init; }
}
