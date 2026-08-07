using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace LTF.App.ViewModels.Screens;

/// <summary>
/// Shared cosmetic display brushes for the M21 in-shell screens, as immutable brushes (thread-safe
/// statics, mirroring <see cref="LTF.App.Converters.SeverityColorConverter"/>). These encode no game
/// state — they are purely how a value is tinted. Team accents come from the brand's fixed data palette
/// (design <c>Colors.axaml</c>), assigned by team order so each team reads as a distinct, stable colour.
/// </summary>
internal static class ScreenBrushes
{
    public static IBrush Primary { get; } = new ImmutableSolidColorBrush(Color.Parse("#F2F4F6"));
    public static IBrush Secondary { get; } = new ImmutableSolidColorBrush(Color.Parse("#C6CBD1"));
    public static IBrush Dim { get; } = new ImmutableSolidColorBrush(Color.Parse("#7D848C"));
    public static IBrush Faint { get; } = new ImmutableSolidColorBrush(Color.Parse("#5C6169"));

    public static IBrush Good { get; } = new ImmutableSolidColorBrush(Color.Parse("#35D07F"));
    public static IBrush Warn { get; } = new ImmutableSolidColorBrush(Color.Parse("#FFB020"));
    public static IBrush Bad { get; } = new ImmutableSolidColorBrush(Color.Parse("#FF3B2F"));

    // The brand's data / category palette (design Colors.axaml: gold, blue, purple, orange, teal, pink).
    private static readonly IBrush[] Palette =
    {
        new ImmutableSolidColorBrush(Color.Parse("#E8B33A")),
        new ImmutableSolidColorBrush(Color.Parse("#3B82F6")),
        new ImmutableSolidColorBrush(Color.Parse("#8B5CF6")),
        new ImmutableSolidColorBrush(Color.Parse("#F97316")),
        new ImmutableSolidColorBrush(Color.Parse("#14B8A6")),
        new ImmutableSolidColorBrush(Color.Parse("#EC4899")),
    };

    /// <summary>A stable accent for the team at <paramref name="index"/> in the carset's team order.</summary>
    public static IBrush TeamAccent(int index) =>
        Palette[((index % Palette.Length) + Palette.Length) % Palette.Length];
}
