using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using LTF.App.Mvvm;
using LTF.App.Services;

namespace LTF.App.ViewModels;

/// <summary>
/// The notification centre: wraps the source's notifications, exposes the unread count (drives the
/// top-bar badge) and re-computes it as items are read. Opening a row is handled by the row itself.
/// </summary>
public sealed partial class InboxViewModel : ViewModelBase
{
    public InboxViewModel(INotificationSource source, INavigationService navigation)
    {
        Items = new ObservableCollection<NotificationViewModel>(
            source.Current().Select(n => new NotificationViewModel(n, key =>
            {
                if (key is { } target)
                {
                    navigation.Navigate(target);
                }
            })));

        foreach (var item in Items)
        {
            item.PropertyChanged += OnItemChanged;
        }

        UpdateUnread();
    }

    public ObservableCollection<NotificationViewModel> Items { get; }

    [ObservableProperty]
    private int _unreadCount;

    public bool HasItems => Items.Count > 0;

    private void OnItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NotificationViewModel.IsRead))
        {
            UpdateUnread();
        }
    }

    private void UpdateUnread() => UnreadCount = Items.Count(i => !i.IsRead);
}
