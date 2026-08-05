using System.Globalization;

namespace LTF.Domain.Common;

/// <summary>
/// A signed relationship strength on a fixed −100…+100 scale (ADR-0013): negative is antagonism,
/// zero is neutral or no established bond, positive is warmth. Kept distinct from <see cref="Rating"/>
/// (a 1–100 ability scale) precisely because affinity must be able to go negative. Immutable and always
/// in range; the struct default is 0 (neutral), so an unset affinity is valid.
/// </summary>
public readonly record struct Affinity
{
    /// <summary>Strongest antagonism.</summary>
    public const int Min = -100;

    /// <summary>Strongest warmth.</summary>
    public const int Max = 100;

    public Affinity(int value)
    {
        if (value is < Min or > Max)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value), value, $"Affinity must be between {Min} and {Max}.");
        }

        Value = value;
    }

    public int Value { get; }

    /// <summary>Neutral — no established relationship.</summary>
    public static Affinity Neutral => new(0);

    /// <summary>Build an affinity, clamping into range instead of throwing.</summary>
    public static Affinity Clamped(int value) => new(Math.Clamp(value, Min, Max));

    /// <summary>This affinity moved by <paramref name="delta"/>, clamped into range.</summary>
    public Affinity Shifted(int delta) => Clamped(Value + delta);

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    public static implicit operator int(Affinity affinity) => affinity.Value;
}
