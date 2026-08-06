using LTF.Domain;

namespace LTF.Career;

/// <summary>Totals accumulated for one team over a research sweep (M14): how far its car's overall level
/// moved across the swept seasons and how many nodes it worked through.</summary>
public sealed record TeamResearchStat
{
    public required string TeamId { get; init; }

    /// <summary>The car's overall level before the sweep began.</summary>
    public required int StartOverall { get; init; }

    /// <summary>The car's overall level after the last swept season.</summary>
    public required int FinalOverall { get; init; }

    /// <summary>Highest overall level seen across the sweep.</summary>
    public required int MaxOverall { get; init; }

    /// <summary>Net change in overall level over the sweep (final − start).</summary>
    public required int OverallGain { get; init; }

    /// <summary>Nodes the team has unlocked by the end of the sweep.</summary>
    public required int NodesUnlocked { get; init; }

    /// <summary>Nodes approved for race (their gain applied) across the sweep.</summary>
    public required int NodesApproved { get; init; }

    /// <summary>Nodes abandoned after running out of rework attempts across the sweep.</summary>
    public required int NodesAbandoned { get; init; }
}

/// <summary>
/// The aggregate result of a research sweep (M14): whether R&amp;D moves the car by a measurable, bounded,
/// deterministic amount over many seasons. It is the R&amp;D counterpart to <see cref="EconomySweepReport"/>
/// — a developing team's car rises, a better-equipped team develops further, and gains stay under the
/// rating ceiling — so the research coefficients can be judged against a real trajectory.
/// </summary>
public sealed record ResearchSweepReport
{
    public required int Seasons { get; init; }

    /// <summary>Per-team totals, ordered by final overall level (strongest first).</summary>
    public required IReadOnlyList<TeamResearchStat> Teams { get; init; }

    /// <summary>The biggest overall gain any team made — the headline "did R&amp;D move the car" number.</summary>
    public required int MaxOverallGain { get; init; }

    /// <summary>Total nodes approved for race across every team.</summary>
    public required int TotalNodesApproved { get; init; }
}

/// <summary>
/// Runs many headless seasons of a career's R&amp;D to check that development balances over time (M14):
/// each season it simulates the championship (<see cref="SeasonSimulator"/>), settles the books
/// (<see cref="EconomyLedger"/>) so teams have money to spend, develops the cars
/// (<see cref="ResearchLedger"/>) and rolls the records forward (<see cref="CareerRollover"/>), which
/// keeps the developed cars and settled finances, tracking every team's overall level across the years.
/// Fully deterministic — each season's seed is derived from the base seed, so the same inputs reproduce
/// the same report. No I/O; the CLI (<c>ltf sweep</c>) wraps this and formats the output.
/// </summary>
public static class ResearchSweep
{
    public static ResearchSweepReport Run(Carset carset, int seasons, int seed)
    {
        if (seasons < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(seasons), seasons, "must be at least one season");
        }

        var start = new Dictionary<string, int>(StringComparer.Ordinal);
        var max = new Dictionary<string, int>(StringComparer.Ordinal);
        var final = new Dictionary<string, int>(StringComparer.Ordinal);
        var unlocked = new Dictionary<string, int>(StringComparer.Ordinal);
        var approved = new Dictionary<string, int>(StringComparer.Ordinal);
        var abandoned = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var team in carset.Teams)
        {
            start[team.Id] = team.Car.Overall;
            max[team.Id] = team.Car.Overall;
        }

        var current = carset;
        for (var season = 0; season < seasons; season++)
        {
            var seasonSeed = SeasonSeed(seed, season);

            // Develop R&D DURING the season (M15): an upgrade approved mid-season is felt in later rounds.
            var progression = new RndProgression(seasonSeed);
            var progress = SeasonSimulator.RunProgressed(current, seasonSeed, progression);
            var settlement = EconomyLedger.SettleSeason(progress.Carset, progress.Result);

            foreach (var team in settlement.Carset.Teams)
            {
                var overall = team.Car.Overall;
                final[team.Id] = overall;
                max[team.Id] = max.TryGetValue(team.Id, out var hi) ? Math.Max(hi, overall) : overall;
                unlocked[team.Id] = team.Research.UnlockedNodeIds.Count;
            }

            foreach (var development in progression.Developments)
            {
                approved[development.TeamId] = approved.GetValueOrDefault(development.TeamId) + development.NodesApproved;
                abandoned[development.TeamId] =
                    abandoned.GetValueOrDefault(development.TeamId) + development.NodesAbandoned;
            }

            // Roll the records forward; CareerRollover keeps the developed cars and settled finances.
            current = CareerRollover.Apply(settlement.Carset, progress.Result);
        }

        var teams = carset.Teams
            .Select(t =>
            {
                var startOverall = start.GetValueOrDefault(t.Id);
                var finalOverall = final.GetValueOrDefault(t.Id, startOverall);
                return new TeamResearchStat
                {
                    TeamId = t.Id,
                    StartOverall = startOverall,
                    FinalOverall = finalOverall,
                    MaxOverall = max.GetValueOrDefault(t.Id, startOverall),
                    OverallGain = finalOverall - startOverall,
                    NodesUnlocked = unlocked.GetValueOrDefault(t.Id),
                    NodesApproved = approved.GetValueOrDefault(t.Id),
                    NodesAbandoned = abandoned.GetValueOrDefault(t.Id),
                };
            })
            .OrderByDescending(s => s.FinalOverall)
            .ThenBy(s => s.TeamId, StringComparer.Ordinal)
            .ToList();

        return new ResearchSweepReport
        {
            Seasons = seasons,
            Teams = teams,
            MaxOverallGain = teams.Count > 0 ? teams.Max(s => s.OverallGain) : 0,
            TotalNodesApproved = teams.Sum(s => s.NodesApproved),
        };
    }

    // A deterministic per-season seed from the base seed and season index (mirrors EconomySweep's mixing).
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
