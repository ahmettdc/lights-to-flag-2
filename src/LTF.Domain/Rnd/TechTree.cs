namespace LTF.Domain.Rnd;

/// <summary>
/// The series-wide development catalog (M14 / ADR-0016): the departments and the graph of nodes that
/// every team develops independently. It is carset content, not career state — a save restores it from
/// the loaded carset. Empty by default, so a carset without a tech tree is inert.
/// </summary>
public sealed record TechTree
{
    public IReadOnlyList<Department> Departments { get; init; } = [];
    public IReadOnlyList<TechNode> Nodes { get; init; } = [];

    /// <summary>An empty catalog — the default for a carset that ships no tech tree.</summary>
    public static TechTree Empty { get; } = new();
}
