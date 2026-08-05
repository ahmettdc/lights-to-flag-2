namespace LTF.Domain.Rnd;

/// <summary>A branch hub of the tech tree (M14 / ADR-0016) — e.g. Aerodynamics, Chassis, Power Unit,
/// Durability. Nodes belong to a department by its <see cref="Id"/>.</summary>
public sealed record Department
{
    public required string Id { get; init; }
    public required string Name { get; init; }
}
