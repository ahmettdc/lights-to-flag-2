using Avalonia;
using Avalonia.Controls.Primitives;

namespace LTF.App.Controls;

/// <summary>Colour intent for a <see cref="Badge"/>.</summary>
public enum BadgeTone
{
    Neutral,
    Accent,
    Good,
    Warn,
    Bad,
}

/// <summary>
/// Small pill showing a short label, tinted by <see cref="Tone"/>. Used for the inbox count, status
/// tags, and category chips across the shell and screens.
/// </summary>
public class Badge : TemplatedControl
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<Badge, string?>(nameof(Text));

    public static readonly StyledProperty<BadgeTone> ToneProperty =
        AvaloniaProperty.Register<Badge, BadgeTone>(nameof(Tone));

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public BadgeTone Tone
    {
        get => GetValue(ToneProperty);
        set => SetValue(ToneProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(Badge);
}
