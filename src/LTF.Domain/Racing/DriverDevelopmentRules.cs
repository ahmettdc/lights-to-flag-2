namespace LTF.Domain.Racing;

/// <summary>
/// The series' driver-development tuning (M18 / ADR-0015): how a driver ages, grows toward their hidden
/// <see cref="Driver.Potential"/> ceiling, and declines past their peak. Like <see cref="ResearchRules"/>
/// and <see cref="EconomyRules"/>, every field defaults to zero, so a carset with no <c>driverDevelopment</c>
/// block ages and develops no one — the inert default, with <see cref="IsActive"/> false.
/// </summary>
public sealed record DriverDevelopmentRules
{
    /// <summary>Age at which a driver's peak band begins; below it a young driver still grows toward their
    /// potential.</summary>
    public int PeakAgeStart { get; init; }

    /// <summary>Age after which a driver declines. Together with the decline coefficients, this bounds the
    /// peak band a driver holds their level through.</summary>
    public int PeakAgeEnd { get; init; }

    /// <summary>Fraction of the gap to <see cref="Driver.Potential"/> a growing driver closes each season
    /// (0 = no growth).</summary>
    public double GrowthPerSeason { get; init; }

    /// <summary>Points a post-peak driver loses each season from the physical attributes — Pace and
    /// Consistency, the fast-fading ones (0 = no physical decline).</summary>
    public double PhysicalDeclinePerSeason { get; init; }

    /// <summary>Points a post-peak driver loses each season from the experience attributes — Racecraft,
    /// TyreManagement, Feedback and WetWeather, the slow-fading ones a "cunning veteran" holds longer
    /// (0 = no experience decline).</summary>
    public double ExperienceDeclinePerSeason { get; init; }

    /// <summary>Seeded spread on a season's growth (0 = deterministic to the curve, no random variation).</summary>
    public double DevelopmentSpread { get; init; }

    /// <summary>Whether the series develops drivers at all. False (the default) leaves every driver's age
    /// and attributes untouched at rollover, so the feature is fully inert until a carset opts in by setting
    /// a growth or decline rate.</summary>
    public bool IsActive =>
        GrowthPerSeason > 0 || PhysicalDeclinePerSeason > 0 || ExperienceDeclinePerSeason > 0;
}
