namespace LTF.Domain.Racing;

/// <summary>
/// The fastest race lap ever set at a circuit over a career (M24): who set it, how quick, and in which year.
/// Maintained per circuit at the season boundary (<c>SeasonArchive.Append</c> keeps the quicker of the standing
/// record and the season's fastest lap there) and stored on <see cref="Carset.TrackRecords"/>, which accumulates
/// across a career and is never reconstructed. Value-typed, so it round-trips a save byte-stably.
/// </summary>
public sealed record TrackRecord
{
    /// <summary>The circuit the record was set at.</summary>
    public required string CircuitId { get; init; }

    /// <summary>The record lap time, in seconds.</summary>
    public required double BestLapSeconds { get; init; }

    /// <summary>The driver who set it (a competitor/driver id).</summary>
    public required string DriverId { get; init; }

    /// <summary>The calendar year it was set.</summary>
    public required int Year { get; init; }
}
