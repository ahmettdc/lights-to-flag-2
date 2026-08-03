using System.Globalization;

namespace LTF.Domain.Common;

/// <summary>
/// A skill / performance rating on a fixed 1–100 scale. Immutable and validated at
/// construction so an out-of-range rating can never exist in the domain.
/// </summary>
public readonly record struct Rating
{
    /// <summary>Lowest legal rating.</summary>
    public const int Min = 1;

    /// <summary>Highest legal rating.</summary>
    public const int Max = 100;

    public Rating(int value)
    {
        if (value is < Min or > Max)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value), value, $"Rating must be between {Min} and {Max}.");
        }

        Value = value;
    }

    public int Value { get; }

    /// <summary>The rating mapped onto 0.0–1.0, for simulation maths.</summary>
    public double Normalized => (Value - Min) / (double)(Max - Min);

    /// <summary>Build a rating, clamping into range instead of throwing.</summary>
    public static Rating Clamped(int value) =>
        new(Math.Clamp(value, Min, Max));

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    public static implicit operator int(Rating rating) => rating.Value;
}
