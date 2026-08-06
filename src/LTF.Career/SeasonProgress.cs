using LTF.Domain;

namespace LTF.Career;

/// <summary>
/// The outcome of one season run with a between-rounds progression (M15): the season
/// <see cref="Result"/> and the <see cref="Carset"/> as it stood at the end of the season, having
/// evolved through the rounds (in-season R&amp;D updates, test days). Shape B — the evolved carset rides
/// alongside the result, exactly as <see cref="SeasonSettlement"/> carries its penalties and
/// <see cref="ResearchOutcome"/> its developments.
/// </summary>
public sealed record SeasonProgress
{
    public required Carset Carset { get; init; }
    public required SeasonResult Result { get; init; }
}
