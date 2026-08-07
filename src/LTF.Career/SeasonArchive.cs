using LTF.Domain;
using LTF.Domain.Racing;

namespace LTF.Career;

/// <summary>
/// Archives a finished season into the carset's lasting history (M24). The per-round results are discarded at the
/// season boundary (<c>LiveCareer.RollToNextSeason</c>), so this is the only place a season's final tables, its
/// champions and its fastest laps are captured before they are lost. It appends one <see cref="SeasonRecord"/> to
/// <see cref="Carset.SeasonHistory"/> and merges each circuit's fastest race lap into <see cref="Carset.TrackRecords"/>
/// (keeping the quicker of the standing record and this season's), then returns the advanced carset. Pure and
/// deterministic, as the Career layer must be: the same season applied to the same carset always yields the same
/// archive, and the produced lists have a stable order so a save round-trips byte-identically.
/// </summary>
public static class SeasonArchive
{
    /// <summary>Return <paramref name="next"/> (the rolled carset) with <paramref name="season"/> archived: one
    /// season record appended to its history and its track records updated. <paramref name="played"/> is the carset
    /// the season was raced on — its calendar maps each round (in order) to a circuit and gives the season's year.</summary>
    public static Carset Append(Carset next, Carset played, SeasonResult season)
    {
        var record = BuildSeasonRecord(played, season);
        var history = next.SeasonHistory.Append(record).ToList();
        var trackRecords = MergeTrackRecords(next.TrackRecords, played, season, record.Year);
        return next with { SeasonHistory = history, TrackRecords = trackRecords };
    }

    // The finished season's year, champions and final championship tables, flattened to the archive's value lines.
    private static SeasonRecord BuildSeasonRecord(Carset played, SeasonResult season)
    {
        var year = played.Calendar.Count > 0 ? played.Calendar[0].Date.Year : 0;

        var drivers = season.Standings.Drivers
            .Select(s => new DriverSeasonLine
            {
                DriverId = s.DriverId,
                Position = s.Position,
                Points = s.Points,
                Wins = s.Wins,
                Podiums = s.Podiums,
            })
            .ToList();

        var constructors = season.Standings.Constructors
            .Select(s => new ConstructorSeasonLine
            {
                TeamId = s.TeamId,
                Position = s.Position,
                Points = s.Points,
                Wins = s.Wins,
            })
            .ToList();

        return new SeasonRecord
        {
            Year = year,
            DriversChampionId = season.DriversChampionId ?? "",
            ConstructorsChampionId = season.ConstructorsChampionId ?? "",
            Drivers = drivers,
            Constructors = constructors,
        };
    }

    // Keep the quicker of the standing record and this season's fastest race lap at each circuit. Output order is
    // deterministic: the existing records first (in place, only their time/holder updated when beaten), then any
    // newly-set circuits appended in calendar order — so a save round-trips byte-identically.
    private static IReadOnlyList<TrackRecord> MergeTrackRecords(
        IReadOnlyList<TrackRecord> existing, Carset played, SeasonResult season, int year)
    {
        // This season's best race lap per circuit, plus the calendar order circuits were first raced in.
        var seasonBest = new Dictionary<string, TrackRecord>(StringComparer.Ordinal);
        var firstSeen = new List<string>();
        var rounds = Math.Min(played.Calendar.Count, season.Rounds.Count);
        for (var i = 0; i < rounds; i++)
        {
            var lap = season.Rounds[i].FastestLapTime;
            var driverId = season.Rounds[i].FastestLapCompetitorId;
            if (lap <= 0 || driverId is null)
            {
                continue;
            }

            var circuitId = played.Calendar[i].CircuitId;
            if (!seasonBest.TryGetValue(circuitId, out var best))
            {
                firstSeen.Add(circuitId);
            }

            if (best is null || lap < best.BestLapSeconds)
            {
                seasonBest[circuitId] = new TrackRecord
                {
                    CircuitId = circuitId, BestLapSeconds = lap, DriverId = driverId, Year = year,
                };
            }
        }

        var existingIds = existing.Select(r => r.CircuitId).ToHashSet(StringComparer.Ordinal);
        var merged = existing
            .Select(r => seasonBest.TryGetValue(r.CircuitId, out var s) && s.BestLapSeconds < r.BestLapSeconds ? s : r)
            .ToList();
        foreach (var circuitId in firstSeen)
        {
            if (!existingIds.Contains(circuitId))
            {
                merged.Add(seasonBest[circuitId]);
            }
        }

        return merged;
    }
}
