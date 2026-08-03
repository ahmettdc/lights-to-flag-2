using LTF.Domain.Common;

namespace LTF.Domain.Management;

/// <summary>
/// The negotiable terms attached to a contract (the mockup's contract screen). Modelled
/// separately so the negotiation layer (M12) can reason about clauses on their own.
/// </summary>
public sealed record ContractClauses
{
    /// <summary>Bonus paid per championship point scored.</summary>
    public long PerPointBonus { get; init; }

    /// <summary>One-off bonus for winning the title.</summary>
    public long ChampionshipBonus { get; init; }

    /// <summary>Buy-out amount that releases the party early; 0 means no exit clause.</summary>
    public long ExitClause { get; init; }

    /// <summary>Whether the driver is guaranteed number-one status in the team.</summary>
    public bool FirstDriverStatus { get; init; }
}

/// <summary>
/// A binding agreement between a team and a driver or staff member. Wages are per season
/// in the carset's currency unit; <see cref="SeasonsRemaining"/> counts down at rollover.
/// </summary>
public sealed record Contract
{
    public required ContractKind Kind { get; init; }

    /// <summary>Id of the driver or staff member this contract binds.</summary>
    public required string PartyId { get; init; }

    /// <summary>Id of the team holding the contract.</summary>
    public required string TeamId { get; init; }

    public required long SalaryPerSeason { get; init; }
    public required int SeasonsRemaining { get; init; }

    public long SigningBonus { get; init; }
    public ContractClauses Clauses { get; init; } = new();

    public bool IsExpiring => SeasonsRemaining <= 1;
}
