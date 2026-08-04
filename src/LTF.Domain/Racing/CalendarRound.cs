namespace LTF.Domain.Racing;

/// <summary>
/// One round of the season calendar: a circuit raced on a specific date, in order.
/// This is the carset-authored schedule (ADR-0011); the career engine (M11) builds its
/// live event queue on top of it. The date is game state, never wall-clock.
/// </summary>
public sealed record CalendarRound
{
    /// <summary>1-based position in the season.</summary>
    public required int Round { get; init; }

    /// <summary>Id of the circuit raced (resolves against <see cref="Carset.Circuits"/>).</summary>
    public required string CircuitId { get; init; }

    /// <summary>The date this round's race is held.</summary>
    public required DateOnly Date { get; init; }

    /// <summary>True when the round includes a sprint race.</summary>
    public bool IsSprint { get; init; }
}
