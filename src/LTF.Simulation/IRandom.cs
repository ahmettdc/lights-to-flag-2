namespace LTF.Simulation;

/// <summary>
/// A deterministic source of randomness. The whole simulation draws through this so a run
/// is reproducible from its seed (ROADMAP §4.2): no wall-clock, no shared global RNG.
/// <see cref="Fork"/> derives independent sub-streams so different parts of a session
/// (each driver, each lap, qualifying vs race) can be seeded without correlating.
/// </summary>
public interface IRandom
{
    /// <summary>A uniform value in [0, 1).</summary>
    double NextDouble();

    /// <summary>A sample from the standard normal distribution (mean 0, stddev 1).</summary>
    double NextGaussian();

    /// <summary>A uniform integer in [minInclusive, maxExclusive).</summary>
    int NextInt(int minInclusive, int maxExclusive);

    /// <summary>
    /// An independent stream derived deterministically from this stream's seed and
    /// <paramref name="salt"/>. Reproducible (same seed + salt → same stream) and
    /// independent of how much this stream has already been consumed.
    /// </summary>
    IRandom Fork(long salt);
}
