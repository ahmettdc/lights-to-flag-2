namespace LTF.Simulation.Racing;

/// <summary>
/// Race-control state applying to the whole field on a given lap. A heavy incident (or a
/// stopped car needing recovery) escalates the race off <see cref="Green"/> into one of the
/// neutralisations and, after a few laps, back to green.
/// </summary>
public enum NeutralizationState
{
    /// <summary>Racing.</summary>
    Green,

    /// <summary>Virtual safety car: the field slows to a delta, gaps are held.</summary>
    VirtualSafetyCar,

    /// <summary>Safety car: the field bunches up behind it and restarts nose to tail.</summary>
    SafetyCar,

    /// <summary>Red flag: the race is stopped; tyres may be changed for free; then a restart.</summary>
    RedFlag,
}
