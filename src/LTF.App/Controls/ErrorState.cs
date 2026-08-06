using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.Primitives;

namespace LTF.App.Controls;

/// <summary>
/// Placeholder shown when a panel failed to load. Carries an optional <see cref="RetryCommand"/> — when
/// set, a Retry button appears. Utility state (uieksikler item 10).
/// </summary>
public class ErrorState : TemplatedControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<ErrorState, string?>(nameof(Title), "Something went wrong");

    public static readonly StyledProperty<string?> MessageProperty =
        AvaloniaProperty.Register<ErrorState, string?>(nameof(Message));

    public static readonly StyledProperty<ICommand?> RetryCommandProperty =
        AvaloniaProperty.Register<ErrorState, ICommand?>(nameof(RetryCommand));

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

    public ICommand? RetryCommand
    {
        get => GetValue(RetryCommandProperty);
        set => SetValue(RetryCommandProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(ErrorState);
}
