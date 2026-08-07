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

    /// <summary>Outstanding bank loans (ADR-0029); empty by default so a team that has never borrowed carries
    /// no debt and is byte-identical to before. Persisted whole with the finances.</summary>
    public IReadOnlyList<Loan> Loans { get; init; } = [];

    /// <summary>Total principal still owed across all loans.</summary>
    public long TotalDebt => Loans.Sum(l => l.OutstandingBalance);

    // Structural equality including the loans list — the compiler-generated record equality would compare
    // Loans by reference, so two finances with the same (or both-empty) loans would wrongly differ. Keeping
    // value semantics matters for round-trip fidelity and any value comparison of teams/finances.
    public bool Equals(Finances? other) =>
        other is not null
        && Balance == other.Balance
        && SeasonBudget == other.SeasonBudget
        && PrizeMoney == other.PrizeMoney
        && SponsorIncome == other.SponsorIncome
        && CostCap == other.CostCap
        && Loans.SequenceEqual(other.Loans);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Balance);
        hash.Add(SeasonBudget);
        hash.Add(PrizeMoney);
        hash.Add(SponsorIncome);
        hash.Add(CostCap);
        foreach (var loan in Loans)
        {
            hash.Add(loan);
        }

        return hash.ToHashCode();
    }
}
