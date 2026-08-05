using LTF.Domain.Common;

namespace LTF.Domain.Racing;

/// <summary>
/// The paddock's web of bonds (ADR-0013): every <see cref="Relationship"/>, each keyed by its
/// canonical (ordinally-smaller, larger) id pair. Part of the career world-state and persisted with a
/// save. Immutable — every change returns a new graph — and pure, so the same career reproduces the
/// same relationships.
/// </summary>
public sealed record RelationshipGraph
{
    public IReadOnlyList<Relationship> Relationships { get; init; } = [];

    /// <summary>An empty graph — the default for a carset with no starting relationships.</summary>
    public static RelationshipGraph Empty { get; } = new();

    /// <summary>The bond between two people, or null if none has formed. Order-agnostic.</summary>
    public Relationship? Between(string x, string y)
    {
        var (a, b) = Canonical(x, y);
        foreach (var r in Relationships)
        {
            if (r.AId == a && r.BId == b)
            {
                return r;
            }
        }

        return null;
    }

    /// <summary>This graph with the bond between two people shifted by <paramref name="delta"/> affinity
    /// (clamped), creating a neutral bond first if none exists yet. Order-agnostic.</summary>
    public RelationshipGraph Shifted(string x, string y, int delta)
    {
        var (a, b) = Canonical(x, y);
        var next = new List<Relationship>(Relationships.Count + 1);
        var found = false;
        foreach (var r in Relationships)
        {
            if (!found && r.AId == a && r.BId == b)
            {
                next.Add(r with { Affinity = r.Affinity.Shifted(delta) });
                found = true;
            }
            else
            {
                next.Add(r);
            }
        }

        if (!found)
        {
            next.Add(new Relationship { AId = a, BId = b, Affinity = Affinity.Clamped(delta) });
        }

        return this with { Relationships = next };
    }

    private static (string A, string B) Canonical(string x, string y) =>
        string.CompareOrdinal(x, y) <= 0 ? (x, y) : (y, x);
}
