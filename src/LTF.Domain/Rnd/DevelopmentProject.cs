namespace LTF.Domain.Rnd;

/// <summary>
/// A team's in-flight development of one <see cref="TechNode"/> (M14 / ADR-0024): its position in the
/// validation pipeline, the design estimate it carries, accumulated progress, and how many rework
/// attempts remain. A project starts <see cref="ValidationState.InDesign"/>; only when it reaches
/// <see cref="ValidationState.ApprovedForRace"/> does its gain become a permanent car rating.
/// </summary>
public sealed record DevelopmentProject
{
    public required string NodeId { get; init; }
    public ValidationState State { get; init; } = ValidationState.InDesign;

    /// <summary>The axis this node develops (copied from the node for a self-contained project).</summary>
    public CarAxis TargetAxis { get; init; }

    /// <summary>Estimated car-rating gain range.</summary>
    public int EstimatedGainMin { get; init; }
    public int EstimatedGainMax { get; init; }

    /// <summary>Design confidence, 0–100.</summary>
    public int Confidence { get; init; } = 100;

    /// <summary>Design-to-track correlation, 0–100.</summary>
    public int CorrelationPercent { get; init; } = 100;

    /// <summary>Progress accumulated toward the next validation state.</summary>
    public int Progress { get; init; }

    /// <summary>Rework attempts left before the project is abandoned.</summary>
    public int RetriesLeft { get; init; }
}
