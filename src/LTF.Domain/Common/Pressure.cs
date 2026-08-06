using System.Globalization;

namespace LTF.Domain.Common;

/// <summary>
/// A pressure or confidence level on a fixed 0–100 scale (ADR-0025 / M17). Kept distinct from
/// <see cref="Rating"/> (a 1–100 ability scale) so a metric can reach an outright floor of 0 — a board's
/// confidence can collapse entirely. Immutable and always in range; the struct default is 0, so an unset
/// pressure is valid.
/// </summary>
public readonly record struct Pressure
{
    /// <summary>The floor — no pressure, or a fully collapsed confidence.</summary>
    public const int Min = 0;

    /// <summary>The ceiling — maximum pressure, or full confidence.</summary>
    public const int Max = 100;

    public Pressure(int value)
    {
        if (value is < Min or > Max)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value), value, $"Pressure must be between {Min} and {Max}.");
        }

        Value = value;
    }

    public int Value { get; }

    /// <summary>The value mapped onto 0.0–1.0.</summary>
    public double Normalized => (Value - Min) / (double)(Max - Min);

    /// <summary>Build a pressure, clamping into range instead of throwing.</summary>
    public static Pressure Clamped(int value) => new(Math.Clamp(value, Min, Max));

    /// <summary>This pressure moved by <paramref name="delta"/>, clamped into range.</summary>
    public Pressure Shifted(int delta) => Clamped(Value + delta);

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    public static implicit operator int(Pressure pressure) => pressure.Value;
}
