using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.Primitives;
using LTF.App.Controls;

namespace LTF.App.Services;

/// <summary>
/// Default <see cref="IDialogService"/>. Adds a <see cref="ConfirmationDialog"/> to the anchor's
/// <see cref="OverlayLayer"/>, awaits the user's choice, then removes it. Uses an in-window overlay
/// rather than a native modal window so it drives cleanly under the headless test platform.
/// </summary>
public sealed class DialogService : IDialogService
{
    public async Task<bool> Confirm(
        Visual anchor,
        string title,
        string message,
        string confirmText = "Confirm",
        string cancelText = "Cancel")
    {
        var layer = OverlayLayer.GetOverlayLayer(anchor);
        if (layer is null)
        {
            return false;
        }

        var dialog = new ConfirmationDialog
        {
            Title = title,
            Message = message,
            ConfirmText = confirmText,
            CancelText = cancelText,
        };

        layer.Children.Add(dialog);
        try
        {
            return await dialog.Result;
        }
        finally
        {
            layer.Children.Remove(dialog);
        }
    }
}
