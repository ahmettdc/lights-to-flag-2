using LTF.Domain.Common;

namespace LTF.Domain.Management;

/// <summary>
/// A team's board and the pressure its principal operates under (ADR-0025 / M17): who owns the team
/// (<see cref="Ownership"/>), the <see cref="Members"/> whose opinions make up board sentiment, the six
/// <see cref="PressureMetrics"/> a season moves, the <see cref="Objectives"/> the season is judged
/// against, and the current <see cref="FiringRisk"/>. Only the player's team carries an evolving board in
/// M17 Core; a carset that ships no boards leaves every team AI-run and a career byte-identical.
/// </summary>
public sealed record TeamBoard
{
    public required string TeamId { get; init; }

    public OwnershipType Ownership { get; init; } = OwnershipType.RacingOwner;

    public IReadOnlyList<BoardMember> Members { get; init; } = [];

    /// <summary>The six pressures on the principal, moved by the board review each season.</summary>
    public PressureMetrics Metrics { get; init; } = PressureMetrics.Neutral;

    public IReadOnlyList<Objective> Objectives { get; init; } = [];

    /// <summary>How close the principal is to the sack (0–100); rises as board confidence collapses.</summary>
    public Pressure FiringRisk { get; init; }
}
