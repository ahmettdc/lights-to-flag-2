using Avalonia;
using LTF.App.Controls;

namespace LTF.App.Services;

/// <summary>Shows transient toast messages in the shell's overlay layer.</summary>
public interface IToastService
{
    /// <summary>Show a toast anchored to the overlay layer of <paramref name="anchor"/>'s visual root.</summary>
    void Show(Visual anchor, string message, ToastTone tone = ToastTone.Info);
}
