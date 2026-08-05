using LTF.Domain;

namespace LTF.Career;

/// <summary>What one team's R&amp;D did over a season (M14): how much budget it spent developing, and which
/// nodes cleared validation and became permanent car gains.</summary>
public sealed record TeamDevelopment
{
    public required string TeamId { get; init; }

    /// <summary>Budget drawn this season to start new development.</summary>
    public long BudgetSpent { get; init; }

    /// <summary>Number of nodes approved for race (permanent gains applied) this season.</summary>
    public int NodesApproved { get; init; }

    /// <summary>Number of nodes abandoned after failing validation this season.</summary>
    public int NodesAbandoned { get; init; }

    /// <summary>Ids of the nodes approved this season.</summary>
    public IReadOnlyList<string> ApprovedNodeIds { get; init; } = [];
}

/// <summary>
/// The result of a season of R&amp;D (M14): the carset with each team's car, finances and research state
/// advanced, plus a per-team development report. Fines-style side artifacts (what got approved) ride
/// alongside the carset, exactly like <see cref="SeasonSettlement"/> carries its penalties.
/// </summary>
public sealed record ResearchOutcome
{
    public required Carset Carset { get; init; }
    public IReadOnlyList<TeamDevelopment> Developments { get; init; } = [];
}
