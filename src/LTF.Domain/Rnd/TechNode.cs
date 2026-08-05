namespace LTF.Domain.Rnd;

/// <summary>
/// One node in the development tree (M14 / ADR-0016): developing it costs money and development quota,
/// requires its prerequisites, and — once validated (ADR-0024) — raises the car rating its
/// <see cref="Category"/> maps to (via <see cref="CarAxisMap"/>) by a gain in [<see cref="GainMin"/>,
/// <see cref="GainMax"/>]. The catalog is series-wide content; per-team progress lives on the team.
/// </summary>
public sealed record TechNode
{
    public required string Id { get; init; }
    public required string DepartmentId { get; init; }
    public CarAxis Category { get; init; }
    public NodeSize Size { get; init; }

    /// <summary>Budget the node draws to develop, in the carset's currency.</summary>
    public long Cost { get; init; }

    /// <summary>Development quota (CFD / wind-tunnel units) the node consumes.</summary>
    public int Quota { get; init; }

    /// <summary>Node ids that must be unlocked before this one.</summary>
    public IReadOnlyList<string> Prerequisites { get; init; } = [];

    /// <summary>Estimated car-rating gain range applied on approval.</summary>
    public int GainMin { get; init; }
    public int GainMax { get; init; }

    /// <summary>Design confidence, 0–100 — how tight the gain estimate is.</summary>
    public int Confidence { get; init; } = 100;

    /// <summary>Design-to-track correlation, 0–100 — how much of the estimate actually realises.</summary>
    public int Correlation { get; init; } = 100;
}
