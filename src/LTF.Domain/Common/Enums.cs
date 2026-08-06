namespace LTF.Domain.Common;

/// <summary>Circuit layout family.</summary>
public enum TrackKind
{
    RoadCourse,
    StreetCircuit,
    Oval,
}

/// <summary>Tyre compound families available to a series.</summary>
public enum TyreCompound
{
    Soft,
    Medium,
    Hard,
    Intermediate,
    Wet,
}

/// <summary>What a team works on in a practice session; the choice shapes the benefit carried
/// into the weekend (M8).</summary>
public enum PracticeProgram
{
    /// <summary>Chase car balance and one-lap pace — the biggest qualifying gain.</summary>
    SetupWork,

    /// <summary>Learn the tyres over a stint — a steadier race-pace gain.</summary>
    TyreEvaluation,

    /// <summary>Rehearse the race — a smaller pace gain, but fewer driver mistakes.</summary>
    RaceSimulation,
}

/// <summary>How a qualifying session decides the grid.</summary>
public enum QualifyingFormat
{
    /// <summary>Multi-part knockout (Q1/Q2/Q3).</summary>
    Knockout,

    /// <summary>One flying lap per driver, run in order.</summary>
    SingleLap,

    /// <summary>One open session; fastest lap counts.</summary>
    SingleSession,
}

/// <summary>Technical / trackside staff roles a team hires (see the UI mockup).</summary>
public enum StaffRole
{
    TechnicalDirector,
    ChiefAerodynamicist,
    ChiefStrategist,
    RaceEngineer,
}

/// <summary>A life-limited car component that carries a per-season usage quota.</summary>
public enum ComponentKind
{
    Engine,
    Gearbox,
    Brakes,
}

/// <summary>Who a contract binds.</summary>
public enum ContractKind
{
    Driver,
    Staff,
}

/// <summary>The tier of a commercial sponsorship deal.</summary>
public enum SponsorTier
{
    Title,
    Primary,
    Secondary,
}

/// <summary>Who owns a team, which sets the board's temperament, budget patience and how much decision
/// latitude the principal is given (ADR-0025 / M17).</summary>
public enum OwnershipType
{
    /// <summary>A racing-focused owner — results-hungry, forgiving of spend.</summary>
    RacingOwner,

    /// <summary>A finance-focused board — guards the budget over the stopwatch.</summary>
    FinanceBoard,

    /// <summary>A manufacturer-backed team — brand and long-term programme matter.</summary>
    ManufacturerBacked,

    /// <summary>A development project — patient, invests in the future and young drivers.</summary>
    DevelopmentProject,

    /// <summary>A sponsor-heavy team — commercial obligations drive decisions.</summary>
    SponsorHeavy,

    /// <summary>A legacy / family owner — heritage and stability over risk.</summary>
    LegacyFamily,
}

/// <summary>How visible a board objective is to the principal (ADR-0025).</summary>
public enum ObjectiveVisibility
{
    /// <summary>Stated plainly up front.</summary>
    Open,

    /// <summary>Held back — judged on but never spelled out.</summary>
    Hidden,

    /// <summary>Negotiable — the board will move it for the right case.</summary>
    Flexible,

    /// <summary>Tied to a budget and risk the board granted for it.</summary>
    Linked,
}

/// <summary>What a board objective measures (ADR-0025): the number a season is judged against.</summary>
public enum ObjectiveKind
{
    /// <summary>Final constructors'-championship position (lower is better).</summary>
    ConstructorPosition,

    /// <summary>Best driver's championship position (lower is better).</summary>
    DriverPosition,

    /// <summary>Constructors'-championship points total.</summary>
    ConstructorPoints,

    /// <summary>End-of-season financial result (stay above a balance).</summary>
    FinancialResult,

    /// <summary>Number of race wins over the season.</summary>
    RaceWins,
}

/// <summary>Dispositions a board member can carry (ADR-0025), as independent flags.</summary>
[Flags]
public enum BoardMemberTraits
{
    None = 0,
    Impatient = 1 << 0,
    PubliclySupportive = 1 << 1,
    RiskAverse = 1 << 2,
    Ambitious = 1 << 3,
}
