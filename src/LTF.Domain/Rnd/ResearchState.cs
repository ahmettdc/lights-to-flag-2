namespace LTF.Domain.Rnd;

/// <summary>
/// A team's R&amp;D progress (M14): the nodes it has unlocked, the projects it is developing, its concept
/// lean, and how ready it is for upcoming regulations. This is career state that evolves and persists;
/// the node catalog it develops against lives on the carset. Empty by default, so a team that never
/// develops anything is inert.
/// </summary>
public sealed record ResearchState
{
    public IReadOnlyList<string> UnlockedNodeIds { get; init; } = [];
    public IReadOnlyList<DevelopmentProject> ActiveProjects { get; init; } = [];
    public ConceptDirection Concept { get; init; } = ConceptDirection.Neutral;

    /// <summary>Preparedness for the next regulation change, 0–100 (ADR-0010 hook).</summary>
    public int RegulationReadiness { get; init; }

    /// <summary>A team that has done no R&amp;D — the default.</summary>
    public static ResearchState Empty { get; } = new();
}
