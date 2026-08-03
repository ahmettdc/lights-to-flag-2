namespace LightsToFlag.Core.Career;

/// <summary>An offer of a seat extended to the player between seasons.</summary>
public sealed record TeamOffer
{
    public int TeamNumber { get; init; }
    public string TeamName { get; init; } = "";
}

/// <summary>A finished season, archived in career history.</summary>
public sealed record SeasonSummary
{
    public int SeasonIndex { get; init; }
    public string DriverChampionId { get; init; } = "";
    public string DriverChampionName { get; init; } = "";
    public string ConstructorChampionName { get; init; } = "";
}

/// <summary>
/// The complete, serializable state of a single-player career. This is the only
/// thing a save file persists (the immutable carset is referenced by name and
/// reloaded). Bump <see cref="SchemaVersion"/> when the shape changes.
/// </summary>
public sealed record CareerState
{
    public int SchemaVersion { get; init; } = 1;

    public string CarsetName { get; init; } = "";
    public int Seed { get; init; }

    /// <summary>The competitor id the human controls, or null to spectate.</summary>
    public string? PlayerId { get; init; }

    public int SeasonIndex { get; init; }

    public IReadOnlyList<SeasonEntrant> Entrants { get; init; } = Array.Empty<SeasonEntrant>();

    /// <summary>Rounds completed in the current season, in order.</summary>
    public IReadOnlyList<RoundResult> CompletedRounds { get; init; } = Array.Empty<RoundResult>();

    /// <summary>Seat offers awaiting the player's decision for next season.</summary>
    public IReadOnlyList<TeamOffer> PendingOffers { get; init; } = Array.Empty<TeamOffer>();

    public IReadOnlyList<SeasonSummary> History { get; init; } = Array.Empty<SeasonSummary>();
}
