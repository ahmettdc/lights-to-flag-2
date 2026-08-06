using LTF.Domain;
using LTF.Domain.Racing;

namespace LTF.Career;

/// <summary>
/// Fills seats left empty by retirement at the transfer window (M18 / ADR-0017) — the writer that finally
/// moves drivers between seats by rewriting <see cref="Team.DriverIds"/> (nothing else does). Each team is
/// offered vacancies worst-first by the finished season's constructors' standings (a defensible draft
/// order), and each vacancy is filled from the best available source: an unseated free agent, else the
/// front of the reserve pool, else a freshly generated rookie (<see cref="RookieGenerator"/>). The hire is
/// signed at the salary they hold out for (via <see cref="ContractNegotiation"/> / <see cref="ContractSigning"/>),
/// so acceptance is deterministic and bounded — no bidding economy (that is deferred). With no vacancy the
/// carset is returned untouched, so a full grid is byte-identical and no transfer happens. Deterministic;
/// no wall-clock, no shared RNG.
/// </summary>
public static class TransferMarket
{
    public static Carset Resolve(
        Carset carset, IReadOnlyDictionary<string, int> seatTargets, Standings standings, int seed)
    {
        var vacancies = carset.Teams.Sum(t => Math.Max(0, Target(seatTargets, t) - t.DriverIds.Count));
        if (vacancies == 0)
        {
            return carset;
        }

        var regenCount = 0;
        foreach (var teamId in WorstFirst(carset, standings))
        {
            var target = Target(seatTargets, Team(carset, teamId));
            while (Team(carset, teamId).DriverIds.Count < target)
            {
                var (hire, fromReserve) = SelectHire(carset, seed, ref regenCount);
                carset = Place(carset, teamId, hire, fromReserve);
            }
        }

        return carset;
    }

    private static (Driver Hire, bool FromReserve) SelectHire(Carset carset, int seed, ref int regenCount)
    {
        var freeAgent = BestFreeAgent(carset);
        if (freeAgent is not null)
        {
            return (freeAgent, false);
        }

        if (carset.Reserves.Count > 0)
        {
            return (carset.Reserves[0], true);
        }

        // No one left in the pool — invent a rookie with an id unique within the carset.
        string id;
        do
        {
            id = $"regen-{seed}-{regenCount++}";
        }
        while (Seated(carset, id) || carset.Drivers.Any(d => string.CompareOrdinal(d.Id, id) == 0));

        return (RookieGenerator.Generate(seed, id), false);
    }

    // Add the hire to the roster (if new), drop a promoted reserve, sign them, and seat them on the team.
    private static Carset Place(Carset carset, string teamId, Driver hire, bool fromReserve)
    {
        var drivers = carset.Drivers.Any(d => string.CompareOrdinal(d.Id, hire.Id) == 0)
            ? carset.Drivers
            : [.. carset.Drivers, hire];
        var reserves = fromReserve
            ? carset.Reserves.Where(d => string.CompareOrdinal(d.Id, hire.Id) != 0).ToList()
            : carset.Reserves;

        var withRoster = carset with { Drivers = drivers, Reserves = reserves };

        // Offer the driver exactly what they hold out for, so the deal always closes.
        var expected = ContractNegotiation.Evaluate(withRoster, hire.Id, long.MaxValue).ExpectedSalary;
        var offer = new ContractOffer
        {
            DriverId = hire.Id,
            TeamId = teamId,
            SalaryPerSeason = expected,
            SeasonsRemaining = 2,
        };
        var signed = ContractSigning.Sign(withRoster, offer).Carset;

        var teams = signed.Teams
            .Select(t => string.CompareOrdinal(t.Id, teamId) == 0
                ? t with { DriverIds = [.. t.DriverIds, hire.Id] }
                : t)
            .ToList();

        return signed with { Teams = teams };
    }

    private static Driver? BestFreeAgent(Carset carset)
    {
        var seated = new HashSet<string>(carset.Teams.SelectMany(t => t.DriverIds), StringComparer.Ordinal);
        return carset.Drivers
            .Where(d => !seated.Contains(d.Id))
            .OrderByDescending(d => d.Attributes.Overall)
            .ThenBy(d => d.Id, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private static bool Seated(Carset carset, string id) =>
        carset.Teams.Any(t => t.DriverIds.Any(seatId => string.CompareOrdinal(seatId, id) == 0));

    // Teams worst-first by constructors' position (highest position number first); ordinal id tiebreak.
    private static IReadOnlyList<string> WorstFirst(Carset carset, Standings standings)
    {
        var position = standings.Constructors.ToDictionary(c => c.TeamId, c => c.Position, StringComparer.Ordinal);
        return carset.Teams
            .Select(t => t.Id)
            .OrderByDescending(id => position.TryGetValue(id, out var p) ? p : int.MaxValue)
            .ThenBy(id => id, StringComparer.Ordinal)
            .ToList();
    }

    private static int Target(IReadOnlyDictionary<string, int> seatTargets, Team team) =>
        seatTargets.TryGetValue(team.Id, out var n) ? n : team.DriverIds.Count;

    private static Team Team(Carset carset, string teamId) =>
        carset.Teams.Single(t => string.CompareOrdinal(t.Id, teamId) == 0);
}
