using Avalonia;
using Avalonia.Controls;

namespace LTF.App.Controls;

/// <summary>
/// Panel primitive: a bordered brand surface with an optional header. The base building block for the
/// dense information panels the Faz-3 screens (M21/M22) are assembled from. Content goes in the usual
/// <see cref="ContentControl.Content"/> slot; set <see cref="Header"/> for a titled panel.
/// </summary>
public class Card : ContentControl
{
    public static readonly StyledProperty<string?> HeaderProperty =
        AvaloniaProperty.Register<Card, string?>(nameof(Header));

    public string? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(Card);
}
