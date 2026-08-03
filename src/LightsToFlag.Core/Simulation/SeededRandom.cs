namespace LightsToFlag.Core.Simulation;

/// <summary>
/// Deterministic <see cref="IRandom"/> backed by a seeded <see cref="Random"/>.
/// Same seed ⇒ identical sequence, which is the backbone of the engine's
/// reproducibility and unit-testability.
/// </summary>
public sealed class SeededRandom : IRandom
{
    private readonly Random _random;
    private double? _spareGaussian;

    public SeededRandom(int seed) => _random = new Random(seed);

    public double NextDouble() => _random.NextDouble();

    public int Next(int maxExclusive) => maxExclusive <= 0 ? 0 : _random.Next(maxExclusive);

    public double NextGaussian()
    {
        if (_spareGaussian is { } spare)
        {
            _spareGaussian = null;
            return spare;
        }

        // Box–Muller: draw a pair, keep one for next time.
        double u1, u2;
        do
        {
            u1 = _random.NextDouble();
        }
        while (u1 <= double.Epsilon);

        u2 = _random.NextDouble();
        var magnitude = Math.Sqrt(-2.0 * Math.Log(u1));
        _spareGaussian = magnitude * Math.Sin(2.0 * Math.PI * u2);
        return magnitude * Math.Cos(2.0 * Math.PI * u2);
    }
}
