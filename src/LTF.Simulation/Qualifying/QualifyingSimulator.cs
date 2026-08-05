using LTF.Domain.Common;
using LTF.Domain.Racing;
using LTF.Simulation.Laps;
using LTF.Simulation.Practice;

namespace LTF.Simulation.Qualifying;

/// <summary>
/// Runs a qualifying session and returns the starting grid. Fully deterministic: each
/// competitor draws its flying laps from its own stream, forked off the session seed, so the
/// same seed and field reproduce the same grid. Three formats (from <see cref="RulesSet.Qualifying"/>):
/// <list type="bullet">
/// <item>Knockout — Q1/Q2/Q3, the slowest cars dropped after each part; the pole battle is the
/// final part.</item>
/// <item>Single-lap — one flying lap each, ordered fastest first.</item>
/// <item>Single-session — best of several laps each, ordered fastest first.</item>
/// </list>
/// Laps are run in low-fuel trim on fresh soft tyres. In a knockout the grid-deciding time is
/// the best lap in the deepest part a driver reached, so a Q2-eliminated car always starts
/// ahead of a Q1-eliminated one regardless of raw lap time. An optional practice-setup map
/// (M8) shaves each car's laps by its qualifying benefit; passing none leaves the grid unchanged.
/// </summary>
public static class QualifyingSimulator
{
    private const long QualifyingSalt = 0x5141_4C46; // "QALF"

    // Knockout advancement: the share of the field that survives each cut.
    private const double Q1AdvanceFraction = 0.75;
    private const double Q2AdvanceFraction = 0.50;

    // Fewer than this and there aren't enough cars for three meaningful knockout parts.
    private const int MinKnockoutField = 3;

    // Laps each driver gets in the single-session format; the best one counts.
    private const int SingleSessionLaps = 3;

    public static QualifyingResult Run(
        Circuit circuit, IReadOnlyList<Competitor> grid, RulesSet rules, BalanceCoefficients balance, int seed,
        IReadOnlyDictionary<string, PracticeSetup>? setups = null)
    {
        var baseRng = new DeterministicRandom(seed);
        var streams = new Dictionary<string, IRandom>(grid.Count, StringComparer.Ordinal);
        for (var i = 0; i < grid.Count; i++)
        {
            // Salt off the race streams so a shared seed gives independent qualifying laps.
            streams[grid[i].Id] = baseRng.Fork(i + 1).Fork(QualifyingSalt);
        }

        return rules.Qualifying switch
        {
            QualifyingFormat.Knockout => Knockout(circuit, grid, balance, streams, setups),
            QualifyingFormat.SingleLap =>
                SingleShot(circuit, grid, balance, streams, laps: 1, QualifyingFormat.SingleLap, setups),
            QualifyingFormat.SingleSession =>
                SingleShot(circuit, grid, balance, streams, SingleSessionLaps, QualifyingFormat.SingleSession, setups),
            _ => throw new ArgumentOutOfRangeException(nameof(rules), rules.Qualifying, "unknown qualifying format"),
        };
    }

    // ---- Knockout (Q1/Q2/Q3) ----------------------------------------------

    private static QualifyingResult Knockout(
        Circuit circuit, IReadOnlyList<Competitor> grid, BalanceCoefficients balance,
        Dictionary<string, IRandom> streams, IReadOnlyDictionary<string, PracticeSetup>? setups)
    {
        var field = grid.Count;
        if (field < MinKnockoutField)
        {
            // Too few cars for three parts — decide it on a single lap, still labelled knockout.
            return SingleShot(circuit, grid, balance, streams, laps: 1, QualifyingFormat.Knockout, setups);
        }

        var advance1 = Math.Clamp((int)Math.Round(field * Q1AdvanceFraction), 1, field - 1);
        var advance2 = Math.Clamp((int)Math.Round(field * Q2AdvanceFraction), 1, advance1 - 1);

        // Part 1: everyone runs; the slowest are knocked out.
        var part1 = RankPart(circuit, grid, balance, streams, setups);
        var toPart2 = part1.Take(advance1).Select(r => r.Competitor).ToList();
        var out1 = part1.Skip(advance1).ToList();

        // Part 2: the Q1 survivors run again.
        var part2 = RankPart(circuit, toPart2, balance, streams, setups);
        var toPart3 = part2.Take(advance2).Select(r => r.Competitor).ToList();
        var out2 = part2.Skip(advance2).ToList();

        // Part 3: the Q2 survivors decide pole.
        var part3 = RankPart(circuit, toPart3, balance, streams, setups);

        var entries = new List<QualifyingEntry>(field);
        var pos = 1;
        Append(entries, part3, part: 3, ref pos);
        Append(entries, out2, part: 2, ref pos);
        Append(entries, out1, part: 1, ref pos);
        return Result(entries, QualifyingFormat.Knockout);
    }

    // ---- Single-lap / single-session --------------------------------------

    private static QualifyingResult SingleShot(
        Circuit circuit, IReadOnlyList<Competitor> grid, BalanceCoefficients balance,
        Dictionary<string, IRandom> streams, int laps, QualifyingFormat format,
        IReadOnlyDictionary<string, PracticeSetup>? setups)
    {
        var ranked = new List<Ran>(grid.Count);
        foreach (var c in grid)
        {
            var (lap, sectors) = BestLap(circuit, c, balance, streams[c.Id], laps, setups);
            ranked.Add(new Ran(c, lap, sectors));
        }

        ranked.Sort(CompareRuns);

        var entries = new List<QualifyingEntry>(ranked.Count);
        var pos = 1;
        Append(entries, ranked, part: 1, ref pos);
        return Result(entries, format);
    }

    // ---- Laps -------------------------------------------------------------

    /// <summary>Rank the given runners by a single flying lap each, fastest first.</summary>
    private static List<Ran> RankPart(
        Circuit circuit, IReadOnlyList<Competitor> runners, BalanceCoefficients balance,
        Dictionary<string, IRandom> streams, IReadOnlyDictionary<string, PracticeSetup>? setups)
    {
        var ranked = new List<Ran>(runners.Count);
        foreach (var c in runners)
        {
            var (lap, sectors) = FlyingLap(circuit, c, balance, streams[c.Id], setups);
            ranked.Add(new Ran(c, lap, sectors));
        }

        ranked.Sort(CompareRuns);
        return ranked;
    }

    /// <summary>The best of <paramref name="laps"/> flying laps for a single competitor.</summary>
    private static (double Lap, SectorTimes Sectors) BestLap(
        Circuit circuit, Competitor competitor, BalanceCoefficients balance, IRandom rng, int laps,
        IReadOnlyDictionary<string, PracticeSetup>? setups)
    {
        var bestLap = double.MaxValue;
        var bestSectors = default(SectorTimes);
        for (var k = 0; k < laps; k++)
        {
            var (lap, sectors) = FlyingLap(circuit, competitor, balance, rng, setups);
            if (lap < bestLap)
            {
                bestLap = lap;
                bestSectors = sectors;
            }
        }

        return (bestLap, bestSectors);
    }

    /// <summary>One qualifying lap: low fuel, fresh softs, dry — a representative pole run, less any
    /// qualifying benefit the car earned in practice.</summary>
    private static (double Lap, SectorTimes Sectors) FlyingLap(
        Circuit circuit, Competitor competitor, BalanceCoefficients balance, IRandom rng,
        IReadOnlyDictionary<string, PracticeSetup>? setups)
    {
        var conditions = new LapConditions
        {
            Tyre = TyreState.Fresh(TyreCompound.Soft),
            FuelFraction = 0.0,
            Track = TrackConditions.Dry,
        };
        var sectors = LapTimeModel.Simulate(circuit, competitor, balance, conditions, rng);
        var bonus = setups is not null && setups.TryGetValue(competitor.Id, out var s) ? s.QualifyingBonusSeconds : 0.0;
        return (sectors.Total - bonus, sectors);
    }

    // ---- Assembly ---------------------------------------------------------

    private static void Append(List<QualifyingEntry> entries, IReadOnlyList<Ran> runs, int part, ref int pos)
    {
        foreach (var r in runs)
        {
            entries.Add(new QualifyingEntry
            {
                GridPosition = pos++,
                CompetitorId = r.Competitor.Id,
                Part = part,
                BestLap = r.Lap,
                BestSectors = r.Sectors,
            });
        }
    }

    private static QualifyingResult Result(List<QualifyingEntry> entries, QualifyingFormat format) => new()
    {
        Grid = entries,
        Format = format,
        PoleCompetitorId = entries.Count > 0 ? entries[0].CompetitorId : null,
        PoleTime = entries.Count > 0 ? entries[0].BestLap : 0.0,
    };

    private static int CompareRuns(Ran a, Ran b)
    {
        var byTime = a.Lap.CompareTo(b.Lap);
        return byTime != 0 ? byTime : string.CompareOrdinal(a.Competitor.Id, b.Competitor.Id);
    }

    private readonly record struct Ran(Competitor Competitor, double Lap, SectorTimes Sectors);
}
