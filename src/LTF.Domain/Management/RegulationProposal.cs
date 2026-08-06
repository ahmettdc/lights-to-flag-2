using LTF.Domain.Rnd;

namespace LTF.Domain.Management;

/// <summary>How a team can respond to a proposed regulation change (ADR-0010 / M17).</summary>
public enum RegulationVote
{
    For,
    Against,
    Abstain,
}

/// <summary>
/// A proposed regulation change put to the teams (ADR-0010 / M17): a fictional rule tweak that would
/// favour development along one car axis by some magnitude. M17 resolves the vote and records the outcome;
/// applying the change and penalising unprepared teams is M18. Fictional (ADR-0003), and the vote is
/// deterministic (ADR-0002).
/// </summary>
public sealed record RegulationProposal
{
    public required string Id { get; init; }

    /// <summary>Short description of the change, for the inbox / UI.</summary>
    public string Description { get; init; } = "";

    /// <summary>The car axis the change favours; teams leaning toward it tend to vote for it.</summary>
    public required CarAxis FavoredAxis { get; init; }

    /// <summary>How large the change is (0–100); recorded for M18 to apply.</summary>
    public int Magnitude { get; init; }
}
