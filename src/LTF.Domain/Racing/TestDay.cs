namespace LTF.Domain.Racing;

/// <summary>
/// A non-race test day on the season calendar (M15 / ADR-0011): a dated running at a circuit that a team
/// uses to develop, separate from a race weekend. Carset-authored, like <see cref="CalendarRound"/>; the
/// career engine surfaces it as a <c>TestDay</c> calendar event and lets development progress there.
/// </summary>
public sealed record TestDay
{
    /// <summary>The date the test is held (game state, never wall-clock).</summary>
    public required DateOnly Date { get; init; }

    /// <summary>Id of the circuit tested at (resolves against <see cref="Carset.Circuits"/>).</summary>
    public required string CircuitId { get; init; }
}
