namespace LTF.Simulation.Racing;

/// <summary>How a competitor's race ended.</summary>
public enum FinishStatus
{
    /// <summary>Reached the flag (possibly a lap or more down).</summary>
    Finished,

    /// <summary>Retired before the end (mechanical, crash, …); see the reason.</summary>
    Retired,

    /// <summary>Never took the start.</summary>
    DidNotStart,
}
