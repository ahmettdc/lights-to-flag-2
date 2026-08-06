using Avalonia;
using Avalonia.Controls.Primitives;

namespace LTF.App.Controls;

/// <summary>
/// Placeholder shown when a list or panel has nothing to show yet ("no project yet", "no eligible
/// driver"). One of the utility states required by the design system (uieksikler item 10).
/// </summary>
public class EmptyState : TemplatedControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<EmptyState, string?>(nameof(Title));

    public static readonly StyledProperty<string?> MessageProperty =
        AvaloniaProperty.Register<EmptyState, string?>(nameof(Message));

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(EmptyState);
}
