using LTF.Domain;
using LTF.Domain.Management;
using LTF.Domain.Racing;

namespace LTF.Career;

/// <summary>A rung on the enforcement (icra) ladder (ADR-0029), in ascending severity.</summary>
public enum EnforcementStep
{
    /// <summary>A missed instalment — a late fee (already capitalised at settlement) and pressure, plus an
    /// action-required warning for the inbox.</summary>
    Warning,

    /// <summary>Repeated misses — the creditor forces the sale of an asset, whose proceeds pay down the debt.</summary>
    AssetSeizure,

    /// <summary>Sustained default — a one-off administrative points penalty on top of the forced sale. Heavy
    /// but survivable: the career always continues (no firing).</summary>
    Insolvency,
}

/// <summary>What the enforcement ladder did to one team this season (ADR-0029): the step reached, any cash a
/// forced sale raised toward the debt and which asset went, and any championship points docked. The
/// mapping to a dated, action-required inbox item is a UI concern (Phase B / M22).</summary>
public sealed record EnforcementAction
{
    public required string TeamId { get; init; }
    public required EnforcementStep Step { get; init; }

    /// <summary>Proceeds a forced sale applied against the team's largest loan (0 when nothing was seized).</summary>
    public long CashRaised { get; init; }

    /// <summary>The asset the creditor seized (e.g. a released staff member's name), or empty.</summary>
    public string SeizedAsset { get; init; } = "";

    /// <summary>Constructor points docked at the terminal step (0 otherwise).</summary>
    public int PointsDocked { get; init; }
}

/// <summary>The result of running the enforcement ladder (ADR-0029): the carset with assets liquidated and
/// board pressure moved, the points penalties to feed <see cref="ConstructorPenalties.Apply"/>, and the
/// per-team actions for the inbox.</summary>
public sealed record EnforcementOutcome
{
    public required Carset Carset { get; init; }

    /// <summary>Administrative points penalties raised this season (empty when none reached the terminal step).</summary>
    public IReadOnlyList<CostCapPenalty> PointsPenalties { get; init; } = [];

    /// <summary>The enforcement actions taken, one per defaulting team (empty when nobody missed).</summary>
    public IReadOnlyList<EnforcementAction> Actions { get; init; } = [];
}

/// <summary>
/// The creditor's escalating pursuit of a defaulting team (ADR-0029, "icra"). Fed the missed instalments a
/// season's <see cref="BankLedger.SettleSeason"/> raised, it climbs a deterministic ladder keyed off each
/// loan's cumulative miss count: a first miss is a <see cref="EnforcementStep.Warning"/> (board pressure +
/// an action-required inbox item); repeated misses force an asset sale whose proceeds pay down the debt;
/// sustained default adds a one-off administrative points penalty (via <see cref="ConstructorPenalties"/>).
/// It is heavy but survivable — the principal is never fired and the career always continues. Pure and
/// deterministic (no RNG); with no misses the carset is returned untouched.
/// </summary>
public static class BankEnforcement
{
    private const int WarningPressure = 8;
    private const int SeizurePressure = 15;
    private const int InsolvencyPressure = 25;

    /// <summary>Escalate against every team that missed an instalment this season and return the updated
    /// carset, the points penalties to apply to the standings, and the actions taken.</summary>
    public static EnforcementOutcome Enforce(Carset carset, IReadOnlyList<MissedPayment> missed)
    {
        if (missed.Count == 0)
        {
            return new EnforcementOutcome { Carset = carset };
        }

        var bank = carset.Rules.Bank;
        var missesByTeam = new Dictionary<string, List<MissedPayment>>(StringComparer.Ordinal);
        foreach (var miss in missed)
        {
            if (!missesByTeam.TryGetValue(miss.TeamId, out var list))
            {
                list = [];
                missesByTeam[miss.TeamId] = list;
            }

            list.Add(miss);
        }

        var teams = new List<Team>(carset.Teams.Count);
        var actions = new List<EnforcementAction>();
        var penalties = new List<CostCapPenalty>();

        foreach (var team in carset.Teams)
        {
            if (!missesByTeam.TryGetValue(team.Id, out var teamMisses))
            {
                teams.Add(team); // paid up this season — untouched
                continue;
            }

            var (updated, action, penalty) = EnforceTeam(team, teamMisses, bank);
            teams.Add(updated);
            actions.Add(action);
            if (penalty is not null)
            {
                penalties.Add(penalty);
            }
        }

        var boards = MovePressure(carset.Boards, actions);

        return new EnforcementOutcome
        {
            Carset = carset with { Teams = teams, Boards = boards },
            PointsPenalties = penalties,
            Actions = actions,
        };
    }

    private static (Team Team, EnforcementAction Action, CostCapPenalty? Penalty) EnforceTeam(
        Team team, List<MissedPayment> misses, BankRules bank)
    {
        var maxTotal = misses.Max(m => m.MissedPaymentsTotal);

        // The terminal points penalty fires once — the season the worst loan first reaches the threshold —
        // not on every later terminal season, so the sporting damage stays a single heavy blow.
        var crossedInsolvency = misses.Any(m => m.MissedPaymentsTotal == bank.InsolvencyAfterMisses);

        var step = maxTotal >= bank.InsolvencyAfterMisses ? EnforcementStep.Insolvency
            : maxTotal >= bank.AssetSeizureAfterMisses ? EnforcementStep.AssetSeizure
            : EnforcementStep.Warning;

        var updated = team;
        long cashRaised = 0;
        var seized = "";
        if (step >= EnforcementStep.AssetSeizure)
        {
            (updated, cashRaised, seized) = Liquidate(updated);
        }

        CostCapPenalty? penalty = null;
        var pointsDocked = 0;
        if (crossedInsolvency && bank.InsolvencyPointsPenalty > 0)
        {
            pointsDocked = bank.InsolvencyPointsPenalty;
            penalty = new CostCapPenalty { TeamId = team.Id, PointsDeducted = pointsDocked };
        }

        var action = new EnforcementAction
        {
            TeamId = team.Id,
            Step = step,
            CashRaised = cashRaised,
            SeizedAsset = seized,
            PointsDocked = pointsDocked,
        };
        return (updated, action, penalty);
    }

    // Force the sale of the team's most valuable saleable asset: release the highest-paid staff member and
    // apply the proceeds (their annual salary) against the largest outstanding loan. A team with no staff
    // has nothing to seize — the other consequences (pressure, any points) still bite.
    private static (Team Team, long Proceeds, string Asset) Liquidate(Team team)
    {
        if (team.Staff.Count == 0)
        {
            return (team, 0, "");
        }

        var sold = team.Staff
            .OrderByDescending(s => s.Salary)
            .ThenBy(s => s.Id, StringComparer.Ordinal)
            .First();
        var proceeds = sold.Salary;
        var remaining = team.Staff.Where(s => !string.Equals(s.Id, sold.Id, StringComparison.Ordinal)).ToList();
        var loans = PayDownLargest(team.Finances.Loans, proceeds);

        var updated = team with
        {
            Staff = remaining,
            Finances = team.Finances with { Loans = loans },
        };
        return (updated, proceeds, sold.FullName);
    }

    // Apply a lump sum to the loan with the largest outstanding balance; a loan it clears drops off the books.
    private static IReadOnlyList<Loan> PayDownLargest(IReadOnlyList<Loan> loans, long amount)
    {
        if (loans.Count == 0 || amount <= 0)
        {
            return loans;
        }

        var idx = 0;
        for (var i = 1; i < loans.Count; i++)
        {
            if (loans[i].OutstandingBalance > loans[idx].OutstandingBalance)
            {
                idx = i;
            }
        }

        var reduced = loans[idx] with { OutstandingBalance = Math.Max(0, loans[idx].OutstandingBalance - amount) };
        var result = loans.ToList();
        if (reduced.IsSettled)
        {
            result.RemoveAt(idx);
        }
        else
        {
            result[idx] = reduced;
        }

        return result;
    }

    // Drop board confidence and raise financial pressure for every team an action hit; a team with no board
    // (every AI team, and some player carsets) simply takes no pressure hit. Firing risk is never touched —
    // enforcement never ends the career.
    private static IReadOnlyList<TeamBoard> MovePressure(
        IReadOnlyList<TeamBoard> boards, List<EnforcementAction> actions)
    {
        if (boards.Count == 0)
        {
            return boards;
        }

        var stepByTeam = new Dictionary<string, EnforcementStep>(StringComparer.Ordinal);
        foreach (var action in actions)
        {
            stepByTeam[action.TeamId] = action.Step;
        }

        return boards
            .Select(b => stepByTeam.TryGetValue(b.TeamId, out var step) ? Pressured(b, step) : b)
            .ToList();
    }

    private static TeamBoard Pressured(TeamBoard board, EnforcementStep step)
    {
        var hit = step switch
        {
            EnforcementStep.Insolvency => InsolvencyPressure,
            EnforcementStep.AssetSeizure => SeizurePressure,
            _ => WarningPressure,
        };

        return board with
        {
            Metrics = board.Metrics with
            {
                BoardConfidence = board.Metrics.BoardConfidence.Shifted(-hit),
                FinancialPressure = board.Metrics.FinancialPressure.Shifted(hit),
            },
        };
    }
}
