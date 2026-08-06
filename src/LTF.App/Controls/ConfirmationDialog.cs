using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace LTF.App.Controls;

/// <summary>
/// Modal-feel confirmation shown in the shell's overlay layer (scrim + centred card). Built and hosted
/// by <see cref="LTF.App.Services.DialogService"/>; awaiting <see cref="Result"/> yields the user's
/// choice. Used before critical decisions (uieksikler item 10).
/// </summary>
public class ConfirmationDialog : TemplatedControl
{
    private readonly TaskCompletionSource<bool> _result = new();

    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<ConfirmationDialog, string?>(nameof(Title));

    public static readonly StyledProperty<string?> MessageProperty =
        AvaloniaProperty.Register<ConfirmationDialog, string?>(nameof(Message));

    public static readonly StyledProperty<string> ConfirmTextProperty =
        AvaloniaProperty.Register<ConfirmationDialog, string>(nameof(ConfirmText), "Confirm");

    public static readonly StyledProperty<string> CancelTextProperty =
        AvaloniaProperty.Register<ConfirmationDialog, string>(nameof(CancelText), "Cancel");

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

    public string ConfirmText
    {
        get => GetValue(ConfirmTextProperty);
        set => SetValue(ConfirmTextProperty, value);
    }

    public string CancelText
    {
        get => GetValue(CancelTextProperty);
        set => SetValue(CancelTextProperty, value);
    }

    /// <summary>Completes with <c>true</c> if confirmed, <c>false</c> if cancelled.</summary>
    public Task<bool> Result => _result.Task;

    /// <summary>Resolve as confirmed. Exposed so tests can drive the dialog without raising input.</summary>
    public void Confirm() => _result.TrySetResult(true);

    /// <summary>Resolve as cancelled.</summary>
    public void Cancel() => _result.TrySetResult(false);

    protected override Type StyleKeyOverride => typeof(ConfirmationDialog);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (e.NameScope.Find<Button>("PART_Confirm") is { } confirm)
        {
            confirm.Click += (_, _) => Confirm();
        }

        if (e.NameScope.Find<Button>("PART_Cancel") is { } cancel)
        {
            cancel.Click += (_, _) => Cancel();
        }
    }
}
