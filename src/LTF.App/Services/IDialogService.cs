using System.Threading.Tasks;
using Avalonia;

namespace LTF.App.Services;

/// <summary>Shows modal confirmations in the shell's overlay layer.</summary>
public interface IDialogService
{
    /// <summary>
    /// Show a confirmation over <paramref name="anchor"/>'s visual root and await the choice
    /// (<c>true</c> = confirmed, <c>false</c> = cancelled or no overlay available).
    /// </summary>
    Task<bool> Confirm(
        Visual anchor,
        string title,
        string message,
        string confirmText = "Confirm",
        string cancelText = "Cancel");
}
