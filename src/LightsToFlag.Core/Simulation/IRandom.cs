namespace LightsToFlag.Core.Simulation;

/// <summary>
/// Source of randomness for the simulation. Everything stochastic in the engine
/// draws from this so a run is fully reproducible from a seed — no static
/// <see cref="System.Random"/> and no wall-clock time anywhere in the engine.
/// </summary>
public interface IRandom
{
    /// <summary>Uniform double in [0, 1).</summary>
    double NextDouble();

    /// <summary>Uniform integer in [0, <paramref name="maxExclusive"/>).</summary>
    int Next(int maxExclusive);

    /// <summary>Standard normal (mean 0, standard deviation 1).</summary>
    double NextGaussian();
}
