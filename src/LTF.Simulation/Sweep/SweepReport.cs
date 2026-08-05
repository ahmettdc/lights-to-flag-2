namespace LTF.Simulation.Sweep;

/// <summary>Totals accumulated for one competitor over a balance sweep (M10).</summary>
public sealed record CompetitorSweepStat
{
    public required string CompetitorId { get; init; }

    /// <summary>Drivers' championships won (most season points).</summary>
    public required int Titles { get; init; }

    /// <summary>Race wins across every simulated race.</summary>
    public required int Wins { get; init; }

    /// <summary>Championship points scored across every race.</summary>
    public required int Points { get; init; }

    /// <summary>Races started.</summary>
    public required int Starts { get; init; }

    /// <summary>Races not finished (retired or did not start).</summary>
    public required int Retirements { get; init; }
}

/// <summary>
/// The aggregate result of a balance sweep: many headless seasons simulated to measure how the
/// current coefficients play out (M10). The report answers the ROADMAP's balance questions —
/// championship spread, win distribution, retirement rate, safety-car frequency and average pit
/// stops — so the coefficients can be tuned against real distributions rather than guesses.
/// </summary>
public sealed record SweepReport
{
    public required int Seasons { get; init; }
    public required int Rounds { get; init; }

    /// <summary>Total races simulated (seasons × rounds).</summary>
    public required int Races { get; init; }

    /// <summary>Per-competitor totals, ordered by titles then points (the championship table).</summary>
    public required IReadOnlyList<CompetitorSweepStat> Competitors { get; init; }

    /// <summary>Fraction of car-races that ended in a retirement / DNS (0..1).</summary>
    public required double RetirementRate { get; init; }

    /// <summary>Average number of safety-car, VSC and red-flag neutralisations per race.</summary>
    public required double SafetyCarsPerRace { get; init; }

    /// <summary>Average number of pit stops made per car-race.</summary>
    public required double AveragePitStopsPerCar { get; init; }
}
