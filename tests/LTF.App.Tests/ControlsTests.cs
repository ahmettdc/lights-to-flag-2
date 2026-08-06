using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using LTF.App.Controls;
using Xunit;

namespace LTF.App.Tests;

/// <summary>
/// Drives the design-system primitives headlessly: each control resolves its brand-themed brushes once
/// attached to a shown window. Proves the ControlThemes/Styles are registered and apply on all 3 OSes.
/// </summary>
public class ControlsTests
{
    private static void Host(Control content)
    {
        var window = new Window { Content = content };
        window.Show();
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void Card_uses_the_brand_surface_brush()
    {
        var card = new Card { Content = new TextBlock { Text = "body" } };
        Host(card);

        var background = Assert.IsAssignableFrom<ISolidColorBrush>(card.Background);
        Assert.Equal(Color.Parse("#14171C"), background.Color);
    }

    [AvaloniaFact]
    public void Badge_tone_selects_the_status_colour()
    {
        var badge = new Badge { Text = "5", Tone = BadgeTone.Good };
        Host(badge);

        var background = Assert.IsAssignableFrom<ISolidColorBrush>(badge.Background);
        Assert.Equal(Color.Parse("#35D07F"), background.Color);
    }

    [AvaloniaFact]
    public void Primary_button_class_applies_its_setters()
    {
        var button = new Button { Content = "GO" };
        button.Classes.Add("primary");
        Host(button);

        Assert.Equal(FontWeight.Bold, button.FontWeight);
    }

    [AvaloniaFact]
    public void Thin_progress_bar_theme_is_registered()
    {
        var app = Application.Current!;
        Assert.True(app.Resources.TryGetResource("ThinProgressBar", app.ActualThemeVariant, out _));
    }
}
