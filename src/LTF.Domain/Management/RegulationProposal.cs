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

    /// <summary>If set, this is a development-freeze proposal (Ri4): passing the ballot freezes
    /// <see cref="FreezeAxes"/> at this mode for the coming season(s), evolving the FIA freeze regime with the
    /// world. Null (the default) → an ordinary car-setback proposal, byte-identical to before. A freeze proposal
    /// with <see cref="Magnitude"/> 0 freezes without also setting the field back.</summary>
    public DevelopmentFreezeMode? FreezeMode { get; init; }

    /// <summary>The car axes a freeze proposal restricts (only read when <see cref="FreezeMode"/> is set).</summary>
    public IReadOnlyList<CarAxis> FreezeAxes { get; init; } = [];
}
