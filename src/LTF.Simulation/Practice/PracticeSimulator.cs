using LTF.Domain.Common;
using LTF.Domain.Racing;

namespace LTF.Simulation.Practice;

/// <summary>
/// Runs a practice session: each car works its chosen <see cref="PracticeProgram"/> and comes
/// away with a <see cref="PracticeSetup"/> for the rest of the weekend. How much benefit a car
/// gets depends on the program, the driver's feedback rating and a seeded session-quality draw
/// (a session can go well or be spoilt), so better feedback and a cleaner run yield more. Fully
/// deterministic: each car draws from its own stream forked off the session seed. A program not
/// named for a car defaults to <see cref="PracticeProgram.SetupWork"/>.
/// </summary>
public static class PracticeSimulator
{
    private const long PracticeSalt = 0x5052_4143; // "PRAC"

    public static PracticeResult Run(
        IReadOnlyList<Competitor> grid, IReadOnlyDictionary<string, PracticeProgram> programs,
        BalanceCoefficients balance, int seed)
    {
        var baseRng = new DeterministicRandom(seed);
        var entries = new List<PracticeEntry>(grid.Count);
        var setups = new Dictionary<string, PracticeSetup>(grid.Count, StringComparer.Ordinal);

        for (var i = 0; i < grid.Count; i++)
        {
            var competitor = grid[i];
            var program = programs.TryGetValue(competitor.Id, out var chosen) ? chosen : PracticeProgram.SetupWork;
            var rng = baseRng.Fork(i + 1).Fork(PracticeSalt);

            var quality = DataQuality(competitor, rng);
            var setup = BuildSetup(program, quality, balance);

            entries.Add(new PracticeEntry
            {
                CompetitorId = competitor.Id,
                Program = program,
                DataQuality = quality,
                Setup = setup,
            });
            setups[competitor.Id] = setup;
        }

        return new PracticeResult { Entries = entries, Setups = setups };
    }

    /// <summary>How much usable data the session produced, 0..1: driven by the driver's feedback
    /// rating and a seeded session-quality factor (traffic, red flags, a spin all cost data).</summary>
    private static double DataQuality(Competitor competitor, IRandom rng)
    {
        var feedback = competitor.Driver.Attributes.Feedback.Normalized;
        var baseline = 0.5 + (0.5 * feedback);
        var sessionFactor = 0.8 + (0.2 * rng.NextDouble());
        return Math.Clamp(baseline * sessionFactor, 0.0, 1.0);
    }

    /// <summary>Turn a program and its data quality into the weekend benefit. Every program helps
    /// both qualifying and the race, weighted to its focus; only race simulation cuts mistakes.</summary>
    private static PracticeSetup BuildSetup(PracticeProgram program, double quality, BalanceCoefficients balance)
    {
        var gain = balance.PracticeSetupGainSeconds * quality;
        return program switch
        {
            PracticeProgram.SetupWork => new PracticeSetup
            {
                QualifyingBonusSeconds = gain,
                RaceBonusSeconds = gain * 0.5,
            },
            PracticeProgram.TyreEvaluation => new PracticeSetup
            {
                QualifyingBonusSeconds = gain * 0.2,
                RaceBonusSeconds = gain * 0.7,
            },
            PracticeProgram.RaceSimulation => new PracticeSetup
            {
                QualifyingBonusSeconds = gain * 0.2,
                RaceBonusSeconds = gain * 0.3,
                ErrorFactor = 1.0 - (balance.PracticeErrorReduction * quality),
            },
            _ => PracticeSetup.None,
        };
    }
}
