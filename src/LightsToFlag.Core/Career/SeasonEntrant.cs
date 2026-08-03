using LightsToFlag.Core.Domain;

namespace LightsToFlag.Core.Career;

/// <summary>
/// A driver's entry for a season: a stable id, the (evolving) driver card, and
/// the team seat they hold. Unlike the immutable carset <see cref="DriverRating"/>,
/// a career mutates these across seasons (ageing, transfers, promotions).
/// </summary>
public sealed record SeasonEntrant
{
    public string Id { get; init; } = "";
    public DriverRating Driver { get; init; } = new() { FirstName = "", LastName = "" };

    /// <summary>1-based index into the carset team list this driver races for.</summary>
    public int TeamNumber { get; init; } = 1;
}
