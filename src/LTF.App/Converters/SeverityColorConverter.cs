using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using LTF.App.Notifications;

namespace LTF.App.Converters;

/// <summary>Maps a <see cref="NotificationSeverity"/> to its dot brush. Immutable brushes so the static
/// initializer is thread-safe (see StatusColorConverter).</summary>
public sealed class SeverityColorConverter : IValueConverter
{
    public static readonly IBrush Info = new ImmutableSolidColorBrush(Color.Parse("#9AA0A8"));
    public static readonly IBrush Success = new ImmutableSolidColorBrush(Color.Parse("#35D07F"));
    public static readonly IBrush Warning = new ImmutableSolidColorBrush(Color.Parse("#FFB020"));
    public static readonly IBrush Critical = new ImmutableSolidColorBrush(Color.Parse("#FF3B2F"));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value switch
        {
            NotificationSeverity.Success => Success,
            NotificationSeverity.Warning => Warning,
            NotificationSeverity.Critical => Critical,
            _ => Info,
        };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
