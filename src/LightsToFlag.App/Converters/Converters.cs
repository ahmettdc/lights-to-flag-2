using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using LightsToFlag.Core.Simulation;

namespace LightsToFlag.App.Converters;

/// <summary>Visible when the bound string equals the ConverterParameter, else Collapsed.</summary>
public sealed class StringEqualsToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        string.Equals(value as string, parameter as string, StringComparison.Ordinal)
            ? Visibility.Visible
            : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>True when the bound string equals the ConverterParameter.</summary>
public sealed class StringEqualsToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        string.Equals(value as string, parameter as string, StringComparison.Ordinal);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true && parameter is string s ? s : Binding.DoNothing;
}

/// <summary>Inverts a boolean.</summary>
public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b && !b;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b && !b;
}

/// <summary>Collapsed when the bound bool is false (True → Visible).</summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Visible when the bound bool is false (True → Collapsed).</summary>
public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Maps a tyre compound to its indicator colour.</summary>
public sealed class TyreBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush Soft = new((Color)ColorConverter.ConvertFromString("#FF3B2F"));
    private static readonly SolidColorBrush Hard = new((Color)ColorConverter.ConvertFromString("#F2F4F6"));
    private static readonly SolidColorBrush Inter = new((Color)ColorConverter.ConvertFromString("#2FBF5B"));
    private static readonly SolidColorBrush Wet = new((Color)ColorConverter.ConvertFromString("#3B82F6"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        TyreCompound.Soft => Soft,
        TyreCompound.Hard => Hard,
        TyreCompound.Intermediate => Inter,
        TyreCompound.Wet => Wet,
        _ => Hard,
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
