using LTF.Domain.Common;

namespace LTF.Domain.Management;

/// <summary>
/// A commercial sponsorship. Pays a per-race fee plus performance bonuses; the economy
/// layer (M13) settles it and objectives can gate the bonuses.
/// </summary>
public sealed record Sponsor
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required SponsorTier Tier { get; init; }

    /// <summary>Guaranteed fee paid per race entered.</summary>
    public long PerRaceFee { get; init; }

    /// <summary>Bonus paid per championship point scored.</summary>
    public long PerPointBonus { get; init; }

    /// <summary>Bonus for meeting the season objective.</summary>
    public long ObjectiveBonus { get; init; }

    /// <summary>Championship position the team must reach to earn <see cref="ObjectiveBonus"/> (0 = none).</summary>
    public int ObjectivePosition { get; init; }
}
