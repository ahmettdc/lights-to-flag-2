using Avalonia;
using Avalonia.Controls.Primitives;

namespace LTF.App.Controls;

/// <summary>Intent for a <see cref="Toast"/> notification.</summary>
public enum ToastTone
{
    Info,
    Success,
    Error,
}

/// <summary>
/// Transient message shown in the shell's overlay layer (e.g. "Saved"). Created and auto-dismissed by
/// <see cref="LTF.App.Services.ToastService"/>. Utility feedback (uieksikler item 10).
/// </summary>
public class Toast : TemplatedControl
{
    public static readonly StyledProperty<string?> MessageProperty =
        AvaloniaProperty.Register<Toast, string?>(nameof(Message));

    public static readonly StyledProperty<ToastTone> ToneProperty =
        AvaloniaProperty.Register<Toast, ToastTone>(nameof(Tone));

    public string? Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public ToastTone Tone
    {
        get => GetValue(ToneProperty);
        set => SetValue(ToneProperty, value);
    }

    protected override Type StyleKeyOverride => typeof(Toast);
}
