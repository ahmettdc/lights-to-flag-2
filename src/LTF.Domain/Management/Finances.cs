namespace LTF.Domain.Management;

/// <summary>
/// A team's financial position (the mockup's Budget). Amounts are whole units of the
/// carset's currency. The economy layer (M13) mutates this between and during seasons.
/// </summary>
public sealed record Finances
{
    /// <summary>Cash in hand. May go negative — the economy layer decides the consequences.</summary>
    public long Balance { get; init; }

    /// <summary>Budget earmarked for the current season.</summary>
    public long SeasonBudget { get; init; }

    /// <summary>Season-to-date prize money.</summary>
    public long PrizeMoney { get; init; }

    /// <summary>Season-to-date sponsorship income.</summary>
    public long SponsorIncome { get; init; }

    /// <summary>Optional cost cap; 0 means the series has no cap.</summary>
    public long CostCap { get; init; }

    public bool HasCostCap => CostCap > 0;
}
