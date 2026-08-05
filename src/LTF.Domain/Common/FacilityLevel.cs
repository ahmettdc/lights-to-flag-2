using System.Globalization;

namespace LTF.Domain.Common;

/// <summary>
/// A facility's build level on a fixed 1–5 scale (M14 / ADR-0020). Immutable and validated at
/// construction, like <see cref="Rating"/>. A higher level gives no car points directly; it makes
/// development faster, more accurate, higher-quality and less risky.
/// </summary>
public readonly record struct FacilityLevel
{
    /// <summary>Lowest legal level.</summary>
    public const int Min = 1;

    /// <summary>Highest legal level.</summary>
    public const int Max = 5;

    public FacilityLevel(int value)
    {
        if (value is < Min or > Max)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value), value, $"Facility level must be between {Min} and {Max}.");
        }

        Value = value;
    }

    public int Value { get; }

    /// <summary>The level mapped onto 0.0–1.0, for development maths.</summary>
    public double Normalized => (Value - Min) / (double)(Max - Min);

    /// <summary>Build a level, clamping into range instead of throwing.</summary>
    public static FacilityLevel Clamped(int value) =>
        new(Math.Clamp(value, Min, Max));

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    public static implicit operator int(FacilityLevel level) => level.Value;
}
