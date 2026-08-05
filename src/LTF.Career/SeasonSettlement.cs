using LTF.Domain;
using LTF.Domain.Management;

namespace LTF.Career;

/// <summary>
/// The result of settling a season's economy (M13): the carset with every team's finances updated, and
/// any cost-cap penalties the settlement produced. Fines are already folded into the balances; the
/// penalties carry the sporting consequences (points, aero-test restriction) for later layers to apply.
/// </summary>
public sealed record SeasonSettlement
{
    /// <summary>The carset with each team's <c>Finances</c> settled for the season.</summary>
    public required Carset Carset { get; init; }

    /// <summary>Cost-cap breach penalties raised this season (empty when nobody overspent).</summary>
    public IReadOnlyList<CostCapPenalty> Penalties { get; init; } = [];
}
