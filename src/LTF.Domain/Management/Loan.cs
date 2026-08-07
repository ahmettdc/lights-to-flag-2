namespace LTF.Domain.Management;

/// <summary>
/// A loan a team has taken from the series' bank (ADR-0029). Amounts are whole units of the carset's
/// currency. The rate is <em>frozen at signing</em> — the bank's offered rate is dynamic (it tracks the
/// team's credit standing each season), but once a loan is drawn it carries the rate it was signed at for
/// its whole life. The economy layer (banking ledger) accrues interest and deducts installments each
/// season; a missed installment capitalises into <see cref="OutstandingBalance"/> and bumps
/// <see cref="MissedPayments"/>, which drives enforcement and the credit score.
/// </summary>
public sealed record Loan
{
    /// <summary>Stable identifier, unique within a team (e.g. "loan-1").</summary>
    public required string Id { get; init; }

    /// <summary>The lender's display name (bank / financier).</summary>
    public string Lender { get; init; } = "";

    /// <summary>The amount originally borrowed.</summary>
    public required long Principal { get; init; }

    /// <summary>Annual interest rate as a whole percent, <em>frozen</em> at the offered rate on the day the
    /// loan was signed.</summary>
    public required int AnnualRatePercent { get; init; }

    /// <summary>The original repayment term, in seasons (sets the per-season principal instalment).</summary>
    public required int TermSeasons { get; init; }

    /// <summary>Seasons left before the loan should be fully repaid.</summary>
    public required int SeasonsRemaining { get; init; }

    /// <summary>Principal still owed. Falls as instalments are paid; a missed instalment capitalises the
    /// unpaid amount plus a late fee back into it (the debt grows).</summary>
    public required long OutstandingBalance { get; init; }

    /// <summary>How many instalments have been missed so far (feeds enforcement + the credit score).</summary>
    public int MissedPayments { get; init; }

    /// <summary>True once nothing is owed — the loan drops off the books.</summary>
    public bool IsSettled => OutstandingBalance <= 0;
}
