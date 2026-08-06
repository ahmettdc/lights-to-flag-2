using LTF.Domain;

namespace LTF.Career;

/// <summary>One team's state at the end of a world sweep (M18): its final car strength and seat roster.</summary>
public sealed record WorldTeamStat
{
    public required string TeamId { get; init; }
    public required int FinalOverall { get; init; }
    public required IReadOnlyList<string> DriverIds { get; init; }
}

/// <summary>
/// The result of a world sweep (M18): how many drivers retired and debuted across the run, each team's final
/// car and roster, and the age spread — enough to show the grid is evolving and staying bounded.
/// </summary>
public sealed record WorldSweepReport
{
    public required int Seasons { get; init; }
    public required int Retirements { get; init; }
    public required int Debuts { get; init; }
    public required IReadOnlyList<WorldTeamStat> Teams { get; init; }
    public required int OldestAge { get; init; }
    public required int YoungestAge { get; init; }
    public required bool AllSeatsFilled { get; init; }
}

/// <summary>
/// Runs a headless multi-season world (M18), the counterpart to <see cref="EconomySweep"/> /
/// <see cref="ResearchSweep"/> / <see cref="BossCareerSweep"/> — but here the whole grid lives: relationships
/// evolve from the season's incidents, drivers age along their development curve, veterans retire, the
/// transfer window fills the vacated seats from the pool (or with generated rookies), and any passed
/// regulation change sets unprepared teams back. Every step is a no-op on a carset that opts into none of
/// it, so an unconfigured world stays put. Fully deterministic — each season derives its seed from the base
/// seed; no I/O, no wall-clock.
/// </summary>
public static class WorldSweep
{
    public static WorldSweepReport Run(Carset carset, int seasons, int seed)
    {
        if (seasons < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(seasons), seasons, "must be at least one season");
        }

        // The seat count each team should carry — captured once, before any driver moves, so the transfer
        // market knows how many vacancies to fill each year.
        var seatTargets = carset.Teams.ToDictionary(t => t.Id, t => t.DriverIds.Count, StringComparer.Ordinal);

        var current = carset;
        var retirements = 0;
        var debuts = 0;

        for (var season = 0; season < seasons; season++)
        {
            var seasonSeed = SeasonSeed(seed, season);
            var before = current.Drivers.Select(d => d.Id).ToHashSet(StringComparer.Ordinal);

            var result = SeasonSimulator.Run(current, seasonSeed);

            current = RelationshipEvolution.Apply(current, result);          // incidents move the paddock
            current = CareerRollover.Apply(current, result);                 // roll the season into records
            current = DriverProgression.Advance(current, seasonSeed);        // age, grow and decline
            current = DriverRetirement.Retire(current);                      // the over-age leave, seats open
            current = ContractLedger.AdvanceSeason(current);                 // contracts count down
            current = TransferMarket.Resolve(current, seatTargets, result.Standings, seasonSeed); // fill seats
            current = RegulationChange.Apply(current, seasonSeed);           // set back the unprepared

            var after = current.Drivers.Select(d => d.Id).ToHashSet(StringComparer.Ordinal);
            retirements += before.Count(id => !after.Contains(id));
            debuts += after.Count(id => !before.Contains(id));
        }

        return BuildReport(seasons, retirements, debuts, current, seatTargets);
    }

    private static WorldSweepReport BuildReport(
        int seasons, int retirements, int debuts, Carset final, IReadOnlyDictionary<string, int> seatTargets)
    {
        var teams = final.Teams
            .Select(t => new WorldTeamStat { TeamId = t.Id, FinalOverall = t.Car.Overall, DriverIds = t.DriverIds })
            .OrderBy(t => t.TeamId, StringComparer.Ordinal)
            .ToList();

        var ages = final.Drivers.Select(d => d.Age).ToList();
        var allSeatsFilled = final.Teams.All(t =>
            t.DriverIds.Count >= (seatTargets.TryGetValue(t.Id, out var n) ? n : t.DriverIds.Count));

        return new WorldSweepReport
        {
            Seasons = seasons,
            Retirements = retirements,
            Debuts = debuts,
            Teams = teams,
            OldestAge = ages.Count > 0 ? ages.Max() : 0,
            YoungestAge = ages.Count > 0 ? ages.Min() : 0,
            AllSeatsFilled = allSeatsFilled,
        };
    }

    // A deterministic per-season seed from the base seed and season index (mirrors the other sweeps).
    private static int SeasonSeed(int baseSeed, int season)
    {
        unchecked
        {
            var h = (uint)baseSeed;
            h = (h ^ (uint)season) * 2654435761u;
            h ^= h >> 16;
            return (int)h;
        }
    }
}
