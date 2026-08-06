using LTF.Domain.Common;

namespace LTF.Domain.Management;

/// <summary>
/// A target the board sets the principal for a season (ADR-0025): what is measured (<see cref="Kind"/>),
/// how visible it is (<see cref="Visibility"/>), the number to hit (<see cref="Target"/>), and — for a
/// linked objective — the budget and risk the board tied to it. The board review assesses it against the
/// season result (M17).
/// </summary>
public sealed record Objective
{
    public required ObjectiveKind Kind { get; init; }

    public ObjectiveVisibility Visibility { get; init; } = ObjectiveVisibility.Open;

    /// <summary>The number to hit — its meaning depends on <see cref="Kind"/> (a championship position, a
    /// points total, a win count, a balance floor…).</summary>
    public required int Target { get; init; }

    /// <summary>Budget the board tied to this objective (0 = none), for a linked objective.</summary>
    public long LinkedBudget { get; init; }

    /// <summary>Risk appetite the board tied to this objective (0–100, 0 = none), for a linked objective.</summary>
    public int LinkedRisk { get; init; }
}
