using Avalonia;
using Avalonia.Controls.Primitives;

namespace LTF.App.Controls;

/// <summary>Placeholder shown while a panel's data is being prepared. Utility state (uieksikler item 10).</summary>
public class LoadingState : TemplatedControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<LoadingState, string?>(nameof(Title), "Loading…");

    public static readonly StyledProperty<string?> MessageProperty =
        AvaloniaProperty.Register<LoadingState, string?>(nameof(Message));

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

    protected override Type StyleKeyOverride => typeof(LoadingState);
}
