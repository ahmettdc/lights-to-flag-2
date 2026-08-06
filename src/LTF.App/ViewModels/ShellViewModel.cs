using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using LTF.App.Mvvm;
using LTF.App.Navigation;
using LTF.App.Services;

namespace LTF.App.ViewModels;

/// <summary>
/// The shell's root view-model: composes the top bar, sidebar, status bar and inbox over one shared
/// <see cref="INavigationService"/>, and owns the inbox popover's open state. Built once at startup (App)
/// and, in tests, from an injected snapshot/notification source. The content region binds to
/// <see cref="INavigationService.CurrentScreen"/> — a placeholder screen in M19; real screens register
/// their factories in M21/M22 without touching this chrome. Opening the inbox is a top-bar toggle;
/// navigating anywhere (a sidebar row or a notification deep link) closes it.
/// </summary>
public sealed partial class ShellViewModel : ViewModelBase
{
    public ShellViewModel(
        INavigationService navigation,
        ISessionSnapshot session,
        INotificationSource notifications)
    {
        Navigation = navigation;
        Inbox = new InboxViewModel(notifications, navigation);
        Sidebar = new SidebarViewModel(navigation);
        StatusBar = new StatusBarViewModel();
        TopBar = new TopBarViewModel(session, Inbox.UnreadCount, () => IsInboxOpen = !IsInboxOpen);

        navigation.PropertyChanged += OnNavigationChanged;
        navigation.Navigate(NavKey.PaddockHub);
    }

    public INavigationService Navigation { get; }

    public TopBarViewModel TopBar { get; }

    public SidebarViewModel Sidebar { get; }

    public StatusBarViewModel StatusBar { get; }

    public InboxViewModel Inbox { get; }

    /// <summary>Whether the inbox popover is open. Toggled from the top bar; closed on any navigation.</summary>
    [ObservableProperty]
    private bool _isInboxOpen;

    private void OnNavigationChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(INavigationService.CurrentKey))
        {
            IsInboxOpen = false;
        }
    }
}
