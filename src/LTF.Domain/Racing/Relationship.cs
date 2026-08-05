using LTF.Domain.Common;

namespace LTF.Domain.Racing;

/// <summary>The character of a bond between two people in the paddock (ADR-0013).</summary>
public enum RelationshipKind
{
    Neutral,
    Rival,
    Ally,
    Mentor,
    Tense,
    Trust,
}

/// <summary>
/// One order-agnostic bond between two people (ADR-0013): a signed <see cref="Affinity"/> plus a
/// label. The pair is stored canonically — <see cref="AId"/> is always the ordinally-smaller id — so
/// a bond reads the same regardless of the order the two ids are given.
/// </summary>
public sealed record Relationship
{
    public required string AId { get; init; }
    public required string BId { get; init; }

    public Affinity Affinity { get; init; }
    public RelationshipKind Kind { get; init; } = RelationshipKind.Neutral;
}
