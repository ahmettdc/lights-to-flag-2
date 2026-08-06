using LTF.Domain;
using LTF.Domain.Common;
using LTF.Domain.Management;
using LTF.Domain.Racing;
using LTF.Simulation;

namespace LTF.Career;

/// <summary>
/// The board's verdict on a season (M17 / ADR-0025). <see cref="Assess"/> takes each board's objectives
/// against the (penalty-adjusted) constructors' standings and the team's finances and evolves the six
/// pressure metrics, every board member's confidence and the firing risk: meeting objectives lifts board
/// confidence and eases pressure, missing them does the reverse, and when confidence collapses the sack
/// looms. Each member reacts to the category — sporting or financial — they weight most, so a board moves
/// as a spread of opinions. Deterministic arithmetic (a seeded jitter only inside the firing danger band,
/// forked per team by a stable ordinal hash, never String.GetHashCode); a carset with no boards is
/// returned untouched.
/// </summary>
public static class BoardReview
{
    private const int ConfidenceSwing = 30; // full swing when every objective is met or missed
    private const int PressureSwing = 25;
    private const int FiringThreshold = 25;  // board confidence below this → firing risk climbs
    private const int FiringJitter = 10;

    /// <summary>Give every board that ships no objectives a default one drawn from its ownership type, so
    /// a season always has something to judge. Boards with authored objectives are left as-is.</summary>
    public static Carset SetObjectives(Carset carset)
    {
        if (carset.Boards.Count == 0)
        {
            return carset;
        }

        var boards = carset.Boards
            .Select(b => b.Objectives.Count > 0 ? b : b with { Objectives = [DefaultObjective(b.Ownership)] })
            .ToList();
        return carset with { Boards = boards };
    }

    /// <summary>Evolve every board against the season's standings and finances. With no boards the carset
    /// is returned untouched.</summary>
    public static Carset Assess(Carset carset, Standings standings, int seed)
    {
        if (carset.Boards.Count == 0)
        {
            return carset;
        }

        var root = new DeterministicRandom(seed);
        var boards = new List<TeamBoard>(carset.Boards.Count);
        foreach (var board in carset.Boards)
        {
            boards.Add(AssessBoard(board, carset, standings, root.Fork(Salt(board.TeamId))));
        }

        return carset with { Boards = boards };
    }

    private static TeamBoard AssessBoard(TeamBoard board, Carset carset, Standings standings, IRandom rng)
    {
        var standing = ConstructorOf(standings, board.TeamId);
        var balance = TeamOf(carset, board.TeamId)?.Finances.Balance ?? 0;
        var driverPos = BestDriverPosition(carset, standings, board.TeamId);

        var overall = MetRatio(board.Objectives, o => IsMet(o, standing, balance, driverPos));
        var sporting = MetRatio(board.Objectives.Where(IsSporting), o => IsMet(o, standing, balance, driverPos));
        var financial = MetRatio(board.Objectives.Where(IsFinancial), o => IsMet(o, standing, balance, driverPos));

        var metrics = board.Metrics with
        {
            BoardConfidence = board.Metrics.BoardConfidence.Shifted(Swing(overall, ConfidenceSwing)),
            SportingPressure = board.Metrics.SportingPressure.Shifted(-Swing(sporting, PressureSwing)),
            FinancialPressure = board.Metrics.FinancialPressure.Shifted(FinancialDelta(financial, balance)),
            SponsorPressure = board.Metrics.SponsorPressure.Shifted(-Swing((sporting + financial) / 2.0, PressureSwing)),
            MediaPressure = board.Metrics.MediaPressure.Shifted(-Swing(sporting, PressureSwing)),
            InternalPressure = board.Metrics.InternalPressure.Shifted(-Swing(overall, PressureSwing)),
        };

        var members = board.Members
            .Select(m => m with { ConfidenceInPlayer = m.ConfidenceInPlayer.Shifted(MemberDelta(m, sporting, financial)) })
            .ToList();

        var firing = FiringRiskOf(metrics.BoardConfidence, board.FiringRisk, rng);

        return board with { Metrics = metrics, Members = members, FiringRisk = firing };
    }

    // A signed swing from a met ratio (0.5 = neutral, 1 = all met, 0 = all missed).
    private static int Swing(double metRatio, int swing) => (int)Math.Round((metRatio - 0.5) * 2.0 * swing);

    // Financial pressure eases when the financial objective is met and rises when the team runs in the red.
    private static int FinancialDelta(double financialMet, long balance) =>
        -Swing(financialMet, PressureSwing) + (balance < 0 ? PressureSwing : 0);

    // A member reacts to the category it weights most heavily.
    private static int MemberDelta(BoardMember member, double sporting, double financial) =>
        Swing(member.FinancialPriority.Value > member.SportingPriority.Value ? financial : sporting, ConfidenceSwing);

    // Firing risk eases while confidence is healthy; in the danger band it climbs by how far confidence has
    // fallen plus a seeded jitter, so a marginal season is not a hard cliff.
    private static Pressure FiringRiskOf(Pressure confidence, Pressure current, IRandom rng)
    {
        if (confidence.Value >= FiringThreshold)
        {
            return current.Shifted(-FiringJitter);
        }

        var climb = (FiringThreshold - confidence.Value) + (int)Math.Round(rng.NextDouble() * FiringJitter);
        return current.Shifted(climb);
    }

    private static double MetRatio(IEnumerable<Objective> objectives, Func<Objective, bool> isMet)
    {
        var total = 0;
        var met = 0;
        foreach (var o in objectives)
        {
            total++;
            if (isMet(o))
            {
                met++;
            }
        }

        return total == 0 ? 0.5 : (double)met / total;
    }

    private static bool IsMet(Objective o, ConstructorStanding? standing, long balance, int driverPos) => o.Kind switch
    {
        ObjectiveKind.ConstructorPosition => standing is not null && standing.Position <= o.Target,
        ObjectiveKind.DriverPosition => driverPos > 0 && driverPos <= o.Target,
        ObjectiveKind.ConstructorPoints => standing is not null && standing.Points >= o.Target,
        ObjectiveKind.FinancialResult => balance >= o.Target,
        ObjectiveKind.RaceWins => standing is not null && standing.Wins >= o.Target,
        _ => false,
    };

    private static bool IsSporting(Objective o) =>
        o.Kind is ObjectiveKind.ConstructorPosition or ObjectiveKind.DriverPosition
            or ObjectiveKind.ConstructorPoints or ObjectiveKind.RaceWins;

    private static bool IsFinancial(Objective o) => o.Kind == ObjectiveKind.FinancialResult;

    /// <summary>The board's opening objective for an ownership type — the anchor a season is judged against
    /// and the starting point <see cref="BoardNegotiation"/> negotiates from.</summary>
    public static Objective DefaultObjective(OwnershipType ownership) => ownership switch
    {
        OwnershipType.RacingOwner => new Objective { Kind = ObjectiveKind.ConstructorPosition, Target = 3 },
        OwnershipType.FinanceBoard => new Objective { Kind = ObjectiveKind.FinancialResult, Target = 0 },
        OwnershipType.ManufacturerBacked => new Objective { Kind = ObjectiveKind.ConstructorPosition, Target = 2 },
        OwnershipType.DevelopmentProject => new Objective { Kind = ObjectiveKind.DriverPosition, Target = 8 },
        OwnershipType.SponsorHeavy => new Objective { Kind = ObjectiveKind.ConstructorPosition, Target = 5 },
        OwnershipType.LegacyFamily => new Objective { Kind = ObjectiveKind.FinancialResult, Target = 0 },
        _ => new Objective { Kind = ObjectiveKind.ConstructorPosition, Target = 5 },
    };

    private static ConstructorStanding? ConstructorOf(Standings standings, string teamId)
    {
        foreach (var c in standings.Constructors)
        {
            if (string.CompareOrdinal(c.TeamId, teamId) == 0)
            {
                return c;
            }
        }

        return null;
    }

    private static Team? TeamOf(Carset carset, string teamId)
    {
        foreach (var t in carset.Teams)
        {
            if (string.CompareOrdinal(t.Id, teamId) == 0)
            {
                return t;
            }
        }

        return null;
    }

    private static int BestDriverPosition(Carset carset, Standings standings, string teamId)
    {
        var team = TeamOf(carset, teamId);
        if (team is null)
        {
            return 0;
        }

        var best = 0;
        foreach (var driverId in team.DriverIds)
        {
            foreach (var d in standings.Drivers)
            {
                if (string.CompareOrdinal(d.DriverId, driverId) == 0 && (best == 0 || d.Position < best))
                {
                    best = d.Position;
                }
            }
        }

        return best;
    }

    // A stable, ordinal FNV-1a hash of an id → fork salt. Never String.GetHashCode (process-randomised).
    private static long Salt(string id)
    {
        unchecked
        {
            var hash = 1469598103934665603UL; // FNV-1a offset basis
            foreach (var c in id)
            {
                hash ^= c;
                hash *= 1099511628211UL; // FNV-1a prime
            }

            return (long)hash;
        }
    }
}
