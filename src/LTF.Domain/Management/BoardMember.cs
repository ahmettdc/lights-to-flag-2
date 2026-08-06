using LTF.Domain.Common;

namespace LTF.Domain.Management;

/// <summary>
/// One member of a team's board (ADR-0025). Each member weights the five priorities differently and
/// carries their own confidence in the principal, risk tolerance and dispositions, so a board reacts as a
/// spread of opinions rather than a single number: a finance-minded member sours on a budget overrun a
/// racing-minded one forgives. Static talent bar <see cref="ConfidenceInPlayer"/>, which a career moves.
/// </summary>
public sealed record BoardMember
{
    public required string Id { get; init; }
    public string Name { get; init; } = "";

    /// <summary>Weight on sporting results (1–100).</summary>
    public required Rating SportingPriority { get; init; }

    /// <summary>Weight on financial control (1–100).</summary>
    public required Rating FinancialPriority { get; init; }

    /// <summary>Weight on long-term investment (1–100).</summary>
    public required Rating LongTermPriority { get; init; }

    /// <summary>Weight on brand and reputation (1–100).</summary>
    public required Rating BrandPriority { get; init; }

    /// <summary>Weight on driver development (1–100).</summary>
    public required Rating DriverDevPriority { get; init; }

    /// <summary>How much this member trusts the principal right now (0–100); a career moves it. Defaults
    /// to a neutral 50.</summary>
    public Pressure ConfidenceInPlayer { get; init; } = new(50);

    /// <summary>Appetite for risk (1–100) — high tolerates bold, cap-stretching calls.</summary>
    public required Rating RiskTolerance { get; init; }

    /// <summary>Dispositions that colour how the member reacts (ADR-0025).</summary>
    public BoardMemberTraits Traits { get; init; } = BoardMemberTraits.None;
}
