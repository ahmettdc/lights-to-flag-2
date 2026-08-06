using System.ComponentModel;
using LTF.App.Navigation;

namespace LTF.App.Services;

/// <summary>Drives which screen the shell's content region shows. Raises change notifications so the
/// sidebar can track the active entry.</summary>
public interface INavigationService : INotifyPropertyChanged
{
    /// <summary>The currently selected navigation target.</summary>
    NavKey CurrentKey { get; }

    /// <summary>The view-model for the current screen (a placeholder in M19).</summary>
    object? CurrentScreen { get; }

    /// <summary>Switch the content region to <paramref name="key"/>.</summary>
    void Navigate(NavKey key);
}
