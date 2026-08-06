using LTF.Domain.Rnd;

namespace LTF.Career;

/// <summary>
/// Per-team development directives (M17): the concept a team steers its R&amp;D toward and a cap on its
/// development spend, read by <see cref="ResearchLedger.DevelopSeason"/> / <see cref="ResearchLedger.DevelopStep"/>.
/// A neutral concept and an uncapped budget leave development byte-identical to passing no directive at all,
/// so a directive only ever changes the teams it actually steers.
/// </summary>
public interface IDevelopmentDirectives
{
    /// <summary>The concept lean to steer team <paramref name="teamId"/>'s node choices toward
    /// (<see cref="ConceptDirection.Neutral"/> = plain catalog order).</summary>
    ConceptDirection ConceptFor(string teamId);

    /// <summary>The most team <paramref name="teamId"/> may spend on development this step
    /// (<see cref="long.MaxValue"/> = uncapped).</summary>
    long BudgetCapFor(string teamId);
}

/// <summary>
/// A Team-Principal's development directive for their own team (M17): the concept they steer toward and a
/// cap on the season's R&amp;D spend. Every other team is left neutral and uncapped, so the AI field develops
/// exactly as it would with no directive. Deterministic — a pure lookup on the team id.
/// </summary>
public sealed class RndDirection : IDevelopmentDirectives
{
    private readonly string _teamId;
    private readonly ConceptDirection _concept;
    private readonly long _budgetCap;

    public RndDirection(string teamId, ConceptDirection concept, long budgetCap = long.MaxValue)
    {
        _teamId = teamId;
        _concept = concept;
        _budgetCap = budgetCap;
    }

    public ConceptDirection ConceptFor(string teamId) =>
        string.CompareOrdinal(teamId, _teamId) == 0 ? _concept : ConceptDirection.Neutral;

    public long BudgetCapFor(string teamId) =>
        string.CompareOrdinal(teamId, _teamId) == 0 ? _budgetCap : long.MaxValue;
}
