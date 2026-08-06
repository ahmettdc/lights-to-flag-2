using LTF.Domain;
using LTF.Domain.Management;
using LTF.Domain.Rnd;

namespace LTF.Career;

/// <summary>
/// The decisions a team principal makes across a career (M17): how to develop the car, who to sign, and
/// how to vote on regulation. The <see cref="BossCareerSweep"/> reads it for the player's team each season.
/// </summary>
public interface IBossPolicy
{
    /// <summary>The R&amp;D directive to develop the player's team under this season.</summary>
    IDevelopmentDirectives Directives(Carset carset);

    /// <summary>Contract offers the boss makes this season (e.g. re-signing an expiring driver).</summary>
    IReadOnlyList<ContractOffer> ContractOffers(Carset carset);

    /// <summary>How the boss votes on a proposed regulation change, or null to vote like the rest.</summary>
    RegulationVote? Vote(Carset carset, RegulationProposal proposal);
}

/// <summary>
/// A configurable team-principal policy (M17): a concept and budget cap to develop under, a fixed set of
/// contract offers to make each season, and a fixed regulation vote. The defaults are neutral — no concept,
/// no cap, no offers, votes with the field — so a default policy drives no player decisions at all, which
/// is how the sweep runs an all-AI career. Pure and deterministic.
/// </summary>
public sealed class BossPolicy : IBossPolicy
{
    private readonly ConceptDirection _concept;
    private readonly long _budgetCap;
    private readonly IReadOnlyList<ContractOffer> _offers;
    private readonly RegulationVote? _vote;

    public BossPolicy(
        ConceptDirection? concept = null,
        long budgetCap = long.MaxValue,
        IReadOnlyList<ContractOffer>? offers = null,
        RegulationVote? vote = null)
    {
        _concept = concept ?? ConceptDirection.Neutral;
        _budgetCap = budgetCap;
        _offers = offers ?? [];
        _vote = vote;
    }

    public IDevelopmentDirectives Directives(Carset carset) =>
        new RndDirection(carset.PlayerTeamId, _concept, _budgetCap);

    public IReadOnlyList<ContractOffer> ContractOffers(Carset carset) => _offers;

    public RegulationVote? Vote(Carset carset, RegulationProposal proposal) => _vote;
}
