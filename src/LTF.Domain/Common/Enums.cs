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
