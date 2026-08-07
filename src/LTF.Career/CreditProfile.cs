using LTF.Domain.Management;
using LTF.Domain.Racing;

namespace LTF.Career;

/// <summary>A coarse credit-rating band (ADR-0029), derived from the 0–100 credit score for the UI.</summary>
public enum CreditTier
{
    Poor,
    Weak,
    Fair,
    Strong,
    Excellent,
}

/// <summary>
/// A team's standing with the series' bank (ADR-0029): a 0–100 credit score derived from the evolving
/// world, the interest rate it would be <em>offered</em> right now, and how much it may borrow. Nothing here
/// is persisted — it is a pure, on-demand function of the (persisted) world state, so it always tracks the
/// world with zero save-format cost. The <see cref="OfferedRatePercent"/> is what a <em>new</em> loan would
/// carry today; an existing loan keeps the rate it was signed at (frozen — see
/// <see cref="Loan.AnnualRatePercent"/>), which is why the offer is dynamic but a drawn loan is not.
/// </summary>
public sealed record CreditProfile
{
    /// <summary>Creditworthiness on a 0–100 scale (higher is safer).</summary>
    public required int Score { get; init; }

    /// <summary>The 5-band label the score falls into, for the UI.</summary>
    public required CreditTier Tier { get; init; }

    /// <summary>Annual interest (percent) a loan signed <em>now</em> would carry — the base rate plus the
    /// risk premium the current score earns. Falls as the score rises.</summary>
    public required int OfferedRatePercent { get; init; }

    /// <summary>The most total debt the bank will let this team carry, given its revenue and score.</summary>
    public required long CreditLimit { get; init; }

    /// <summary>Debt already outstanding across the team's loans.</summary>
    public required long TotalDebt { get; init; }

    /// <summary>How much more the team may borrow right now (never negative).</summary>
    public long Headroom => Math.Max(0, CreditLimit - TotalDebt);

    /// <summary>Whether there is any room to borrow.</summary>
    public bool CanBorrow => Headroom > 0;

    // Component weights. They sum to 100 when a board is present; without one the governance signal is
    // dropped (weight 0) and the remaining signals are renormalised, so a team with no board still scores.
    private const int OnTrackWeight = 25;
    private const int SolvencyWeight = 30;
    private const int CarWeight = 15;
    private const int GovernanceWeight = 15;
    private const int RepaymentWeight = 15;

    // Score → tier band cut-offs.
    private const int ExcellentFrom = 80;
    private const int StrongFrom = 65;
    private const int FairFrom = 50;
    private const int WeakFrom = 35;

    /// <summary>
    /// Derive a team's live credit profile from the evolving world: its championship standing (a proxy for
    /// future prize money), cash solvency, car strength (a proxy for future results), board stability, and
    /// loan-repayment history. <paramref name="board"/> is optional — only the player's team carries one
    /// (M17 Core); without it the governance signal is dropped and the remaining signals carry the score.
    /// Because every input is re-read each time, the profile moves in lock-step with the world. Pure and
    /// deterministic (integer arithmetic, no RNG).
    /// </summary>
    public static CreditProfile For(
        Team team,
        Standings? standings,
        TeamBoard? board,
        BankRules bank,
        EconomyRules economy)
    {
        var standing = ConstructorOf(standings, team.Id);
        var fieldSize = standings?.Constructors.Count ?? 0;
        var revenue = ServiceableRevenue(team.Finances, economy);

        var score = Blend(
            (OnTrackScore(standing, fieldSize), OnTrackWeight),
            (SolvencyScore(team.Finances.Balance, revenue), SolvencyWeight),
            (CarScore(team.Car), CarWeight),
            (GovernanceScore(board), board is null ? 0 : GovernanceWeight),
            (RepaymentScore(team.Finances.Loans), RepaymentWeight));

        return new CreditProfile
        {
            Score = score,
            Tier = TierOf(score),
            OfferedRatePercent = OfferedRate(score, bank),
            CreditLimit = CreditLimitOf(revenue, score, bank),
            TotalDebt = team.Finances.TotalDebt,
        };
    }

    /// <summary>Serviceable annual revenue the bank lends against: last season's prize + sponsorship plus the
    /// flat TV income every team receives.</summary>
    private static long ServiceableRevenue(Finances finances, EconomyRules economy) =>
        finances.PrizeMoney + finances.SponsorIncome + economy.TvIncome;

    // A weighted average of 0–100 component scores; a zero-weight component (e.g. an absent board) drops out.
    private static int Blend(params (int Score, int Weight)[] components)
    {
        long weighted = 0;
        long totalWeight = 0;
        foreach (var (score, weight) in components)
        {
            weighted += (long)score * weight;
            totalWeight += weight;
        }

        return totalWeight == 0 ? 50 : Clamp((int)(weighted / totalWeight));
    }

    // Better championship position ⇒ more future prize money ⇒ safer. Linear: P1 → 100, last → 0. No
    // standing yet (pre-season) is neutral.
    private static int OnTrackScore(ConstructorStanding? standing, int fieldSize)
    {
        if (standing is null || fieldSize <= 1)
        {
            return 50;
        }

        return Clamp(100 - ((standing.Position - 1) * 100 / (fieldSize - 1)));
    }

    // Cash against a year's revenue: a full year in hand → 100, a year in the red → 0, break-even → 50.
    private static int SolvencyScore(long balance, long revenue)
    {
        if (revenue <= 0)
        {
            return balance > 0 ? 60 : balance < 0 ? 20 : 50;
        }

        return Clamp((int)(50 + (balance * 50 / revenue)));
    }

    // A stronger car predicts stronger results ⇒ more future income.
    private static int CarScore(Car car) => Clamp(car.Overall);

    // A principal the board backs and who is far from the sack is a safer bet.
    private static int GovernanceScore(TeamBoard? board)
    {
        if (board is null)
        {
            return 50;
        }

        var confidence = board.Metrics.BoardConfidence.Value;
        var safety = 100 - board.FiringRisk.Value;
        return Clamp((confidence + safety) / 2);
    }

    // The strongest real-bank signal: every missed instalment across the team's loans dents the score.
    private static int RepaymentScore(IReadOnlyList<Loan> loans)
    {
        var misses = 0;
        foreach (var loan in loans)
        {
            misses += loan.MissedPayments;
        }

        return Clamp(100 - (misses * 20));
    }

    private static CreditTier TierOf(int score) => score switch
    {
        >= ExcellentFrom => CreditTier.Excellent,
        >= StrongFrom => CreditTier.Strong,
        >= FairFrom => CreditTier.Fair,
        >= WeakFrom => CreditTier.Weak,
        _ => CreditTier.Poor,
    };

    // Offered rate = base + a risk premium that scales from the full premium (score 0) down to nothing
    // (score 100), so the best credit pays the base rate and the worst pays a loan-shark premium.
    private static int OfferedRate(int score, BankRules bank) =>
        bank.BaseRatePercent + (bank.MaxRiskPremiumPercent * (100 - score) / 100);

    // Limit = a multiple of revenue, scaled by score from half capacity (score 0) to full (score 100). An
    // inert bank or a team with no revenue can borrow nothing.
    private static long CreditLimitOf(long revenue, int score, BankRules bank)
    {
        if (!bank.IsActive || revenue <= 0)
        {
            return 0;
        }

        var capacity = revenue * bank.MaxLoanToRevenuePercent / 100;
        var multiplierPercent = 50 + (score / 2);
        return capacity * multiplierPercent / 100;
    }

    private static ConstructorStanding? ConstructorOf(Standings? standings, string teamId)
    {
        if (standings is null)
        {
            return null;
        }

        foreach (var c in standings.Constructors)
        {
            if (string.CompareOrdinal(c.TeamId, teamId) == 0)
            {
                return c;
            }
        }

        return null;
    }

    private static int Clamp(int value) => Math.Clamp(value, 0, 100);
}
