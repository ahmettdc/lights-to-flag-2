using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Xunit;

namespace LTF.App.Tests;

/// <summary>
/// Proves the brand theme (Colors / Brushes / Typography) is merged into the real <see cref="App"/> and
/// resolves under the headless platform on all three CI OSes — the M19c "palette + fonts wired" gate.
/// Also proves App.axaml still loads after the theme was layered on top of FluentTheme.
/// </summary>
public class ThemeTests
{
    [AvaloniaFact]
    public void Brand_brushes_resolve_with_expected_colours()
    {
        var app = Application.Current!;
        var variant = app.ActualThemeVariant;

        Assert.True(app.Resources.TryGetResource("BrushAccent", variant, out var accent));
        var brush = Assert.IsAssignableFrom<ISolidColorBrush>(accent);
        Assert.Equal(Color.Parse("#FF3B2F"), brush.Color);

        Assert.True(app.Resources.TryGetResource("BrushAppBackground", variant, out _));
        Assert.True(app.Resources.TryGetResource("BrushCard", variant, out _));
        Assert.True(app.Resources.TryGetResource("BrushStatusGood", variant, out _));
        Assert.True(app.Resources.TryGetResource("BrushTextPrimary", variant, out _));
    }

    [AvaloniaFact]
    public void Brand_font_families_resolve()
    {
        var app = Application.Current!;
        var variant = app.ActualThemeVariant;

        Assert.True(app.Resources.TryGetResource("FontHeading", variant, out var headingObj));
        var heading = Assert.IsType<FontFamily>(headingObj);
        Assert.Equal("Saira Condensed", heading.Name);

        Assert.True(app.Resources.TryGetResource("FontNumeric", variant, out _));
        Assert.True(app.Resources.TryGetResource("FontBody", variant, out _));
    }
}
