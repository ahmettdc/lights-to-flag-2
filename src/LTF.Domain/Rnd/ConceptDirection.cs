namespace LTF.Domain.Rnd;

/// <summary>
/// A team's car-concept lean (M14 / ADR-0016): the philosophy its development pushes toward. Leans are
/// signed, −100 … +100, with 0 balanced. They give teams identity and a small opportunity cost — a node
/// aligned with the concept realises a touch more reliably than one against it. Neutral by default.
/// </summary>
public sealed record ConceptDirection
{
    /// <summary>Aero lean: −100 low-drag … +100 high-downforce; 0 balanced.</summary>
    public int AeroLean { get; init; }

    /// <summary>Powertrain lean: −100 efficiency … +100 peak power; 0 balanced.</summary>
    public int PowertrainLean { get; init; }

    /// <summary>A balanced, undifferentiated concept — the default.</summary>
    public static ConceptDirection Neutral { get; } = new();
}
