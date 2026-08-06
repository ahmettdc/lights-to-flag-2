using LTF.Domain.Management;

namespace LTF.Career;

/// <summary>
/// Applies cost-cap points deductions to the constructors' championship (M17 / ADR-0010). The economy
/// layer produces a <see cref="CostCapPenalty"/> with the points to dock (M13) but leaves them unapplied;
/// this subtracts them from the matching constructor line and re-sorts and re-numbers the table by the
/// same keys the championship uses (points, then wins, then id). Pure and deterministic; with no penalties
/// — or only zero-point ones — the standings are returned unchanged.
/// </summary>
public static class ConstructorPenalties
{
    /// <summary>Return the standings with every penalty's points deducted from its constructor and the
    /// table re-ordered; the same standings instance when nothing is deducted.</summary>
    public static Standings Apply(Standings standings, IReadOnlyList<CostCapPenalty> penalties)
    {
        if (penalties.Count == 0)
        {
            return standings;
        }

        var deduction = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var penalty in penalties)
        {
            if (penalty.PointsDeducted != 0)
            {
                deduction[penalty.TeamId] = deduction.GetValueOrDefault(penalty.TeamId) + penalty.PointsDeducted;
            }
        }

        if (deduction.Count == 0)
        {
            return standings;
        }

        var constructors = standings.Constructors
            .Select(c => c with { Points = c.Points - deduction.GetValueOrDefault(c.TeamId) })
            .OrderByDescending(c => c.Points)
            .ThenByDescending(c => c.Wins)
            .ThenBy(c => c.TeamId, StringComparer.Ordinal)
            .Select((c, i) => c with { Position = i + 1 })
            .ToList();

        return standings with { Constructors = constructors };
    }
}
