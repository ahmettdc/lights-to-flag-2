namespace LTF.Simulation;

/// <summary>
/// A small, fast, fully deterministic PRNG (SplitMix64). Given the same seed it always
/// produces the same sequence on every platform, which is the backbone of reproducible
/// simulation. Not cryptographic — it does not need to be.
/// </summary>
public sealed class DeterministicRandom : IRandom
{
    private const ulong Gamma = 0x9E3779B97F4A7C15UL;

    private readonly ulong _seed;
    private ulong _state;

    public DeterministicRandom(long seed)
    {
        _seed = unchecked((ulong)seed);
        _state = _seed;
    }

    public double NextDouble() =>
        // Top 53 bits → a double in [0, 1) with full mantissa precision.
        (NextRaw() >> 11) * (1.0 / 9007199254740992.0);

    public double NextGaussian()
    {
        // Box–Muller. u1 in (0, 1] keeps the logarithm finite.
        var u1 = 1.0 - NextDouble();
        var u2 = NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }

    public int NextInt(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxExclusive), maxExclusive, "maxExclusive must be greater than minInclusive.");
        }

        var range = (ulong)((long)maxExclusive - minInclusive);
        return (int)(minInclusive + (long)(NextRaw() % range));
    }

    public IRandom Fork(long salt)
    {
        // Derive a child seed from the immutable seed + salt (not the mutable state), so a
        // fork is reproducible by name and independent of how much the parent has consumed.
        var mixed = Mix(_seed ^ unchecked(Gamma * (ulong)salt));
        return new DeterministicRandom(unchecked((long)mixed));
    }

    private ulong NextRaw()
    {
        unchecked
        {
            _state += Gamma;
            return Mix(_state);
        }
    }

    private static ulong Mix(ulong z)
    {
        unchecked
        {
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }
}
