using LTF.Domain.Common;

namespace LTF.Domain.Management;

/// <summary>
/// A team's development infrastructure (the mockup's Wind Tunnel / Simulator /
/// Correlation). Levels feed the R&D layer (M14); <see cref="Correlation"/> governs how
/// well development gains carry from the factory to the track.
/// </summary>
public sealed record Facilities
{
    /// <summary>Wind-tunnel quality.</summary>
    public required Rating WindTunnel { get; init; }

    /// <summary>Driver-in-the-loop simulator quality.</summary>
    public required Rating Simulator { get; init; }

    /// <summary>Overall factory / manufacturing capability.</summary>
    public required Rating Factory { get; init; }

    /// <summary>
    /// How faithfully factory gains translate to on-track pace. Low correlation means
    /// upgrades can underdeliver or backfire.
    /// </summary>
    public required Rating Correlation { get; init; }

    /// <summary>A neutral mid-level facility set, for construction without full detail.</summary>
    public static Facilities Default { get; } = new()
    {
        WindTunnel = new Rating(50),
        Simulator = new Rating(50),
        Factory = new Rating(50),
        Correlation = new Rating(50),
    };
}
