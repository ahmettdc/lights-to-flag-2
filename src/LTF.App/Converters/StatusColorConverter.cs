using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace LTF.App.Converters;

/// <summary>
/// Maps a 0–100 metric to the brand status brush: &gt;= 80 good (green), &gt;= 70 warning (amber),
/// otherwise critical (red). Thresholds and colours mirror the mockup's <c>rate()</c> rule
/// (design/mockups/ui.dc.html) and the brand kit (design/brand/README.txt).
/// Self-contained fixed brushes — unit-testable without the theme or a running app; the app is
/// Dark-only, so these status colours never vary by theme variant.
/// </summary>
public sealed class StatusColorConverter : IValueConverter
{
    public const double GoodThreshold = 80;
    public const double WarnThreshold = 70;

    // Immutable brushes so the static initializer is thread-safe: unlike SolidColorBrush (an
    // AvaloniaObject with dispatcher affinity), ImmutableSolidColorBrush can be constructed on any
    // thread, so the plain [Fact] converter tests don't hit "Call from invalid thread".

    /// <summary>Green #35D07F — metric is healthy.</summary>
    public static readonly IBrush Good = new ImmutableSolidColorBrush(Color.Parse("#35D07F"));

    /// <summary>Amber #FFB020 — metric is a risk / warning.</summary>
    public static readonly IBrush Warn = new ImmutableSolidColorBrush(Color.Parse("#FFB020"));

    /// <summary>Red #FF3B2F — metric is critical.</summary>
    public static readonly IBrush Bad = new ImmutableSolidColorBrush(Color.Parse("#FF3B2F"));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        Classify(value);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    /// <summary>The threshold rule, shared by <see cref="Convert"/> and its tests — one source of truth.</summary>
    public static IBrush Classify(object? value)
    {
        var n = ToDouble(value);
        if (double.IsNaN(n)) return Bad;
        if (n >= GoodThreshold) return Good;
        if (n >= WarnThreshold) return Warn;
        return Bad;
    }

    private static double ToDouble(object? value)
    {
        try
        {
            return value switch
            {
                null => double.NaN,
                double d => d,
                IConvertible c => c.ToDouble(CultureInfo.InvariantCulture),
                _ => double.NaN,
            };
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            return double.NaN;
        }
    }
}
