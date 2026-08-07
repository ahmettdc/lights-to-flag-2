using Avalonia;

namespace LTF.App.ViewModels.Screens;

/// <summary>
/// Maps data coordinates onto a fixed drawing canvas for the hand-built charts (M24) — the same role
/// <see cref="TrackOutline"/> plays for the track map, but for arbitrary (x, y) series. X grows left→right and Y
/// is flipped so a larger value sits higher on screen, with a uniform <c>pad</c> inset on every side. A degenerate
/// axis (min == max) collapses to the low edge rather than dividing by zero, so a single-round or all-equal series
/// still resolves to valid points. Pure and deterministic, so a chart is reproducible and unit-testable without a
/// view.
/// </summary>
public readonly struct ChartScale
{
    private readonly double _minX;
    private readonly double _spanX;
    private readonly double _minY;
    private readonly double _spanY;
    private readonly double _width;
    private readonly double _height;
    private readonly double _pad;

    public ChartScale(double minX, double maxX, double minY, double maxY, double width, double height, double pad)
    {
        _minX = minX;
        _spanX = maxX - minX;
        _minY = minY;
        _spanY = maxY - minY;
        _width = width;
        _height = height;
        _pad = pad;
    }

    /// <summary>The canvas X for a data X (min → left inset, max → right inset).</summary>
    public double X(double x) =>
        _pad + (_spanX <= 0 ? 0.0 : (x - _minX) / _spanX * (_width - (2 * _pad)));

    /// <summary>The canvas Y for a data Y (min → bottom inset, max → top inset — Y is flipped).</summary>
    public double Y(double y) =>
        _height - _pad - (_spanY <= 0 ? 0.0 : (y - _minY) / _spanY * (_height - (2 * _pad)));

    /// <summary>The canvas point for a data point.</summary>
    public Point At(double x, double y) => new(X(x), Y(y));
}
