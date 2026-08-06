using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using LTF.App.Controls;

namespace LTF.App.Services;

/// <summary>
/// Default <see cref="IToastService"/>. Adds a <see cref="Toast"/> to the anchor's
/// <see cref="OverlayLayer"/> and removes it after a short delay. The auto-dismiss timer is display-only
/// (never feeds game state), consistent with the layering rules.
/// </summary>
public sealed class ToastService : IToastService
{
    private static readonly TimeSpan Duration = TimeSpan.FromSeconds(3);

    public void Show(Visual anchor, string message, ToastTone tone = ToastTone.Info)
    {
        var layer = OverlayLayer.GetOverlayLayer(anchor);
        if (layer is null)
        {
            return;
        }

        var toast = new Toast { Message = message, Tone = tone };
        layer.Children.Add(toast);

        DispatcherTimer.RunOnce(() => layer.Children.Remove(toast), Duration);
    }
}
