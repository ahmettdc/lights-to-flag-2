using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using LTF.App.Mvvm;
using LTF.App.Navigation;
using LTF.App.Services;

namespace LTF.App.ViewModels;

/// <summary>
/// The notification centre: wraps the source's notifications, exposes the unread count (drives the
/// top-bar badge) and re-computes it as items are read. Opening a row is handled by the row itself. When the
/// source is appendable (M21), the inbox subscribes and adds newly-pushed items as the career advances,
/// keeping the read state of items already shown.
/// </summary>
public sealed partial class InboxViewModel : ViewModelBase
{
    private readonly INotificationSource _source;
    private readonly Action<NavKey?> _navigate;
    private readonly HashSet<string> _known = new(StringComparer.Ordinal);

    public InboxViewModel(INotificationSource source, INavigationService navigation)
    {
        _source = source;
        _navigate = key =>
        {
            if (key is { } target)
            {
                navigation.Navigate(target);
            }
        };

        Items = new ObservableCollection<NotificationViewModel>();
        Sync();

        if (source is IMutableNotificationSource mutable)
        {
            mutable.Changed += Sync;
        }
    }

    public ObservableCollection<NotificationViewModel> Items { get; }

    [ObservableProperty]
    private int _unreadCount;

    public bool HasItems => Items.Count > 0;

    // Add any source items not already shown (by id), preserving the read state of existing rows.
    private void Sync()
    {
        var added = false;
        foreach (var notification in _source.Current())
        {
            if (_known.Add(notification.Id))
            {
                var row = new NotificationViewModel(notification, _navigate);
                row.PropertyChanged += OnItemChanged;
                Items.Add(row);
                added = true;
            }
        }

        if (added)
        {
            OnPropertyChanged(nameof(HasItems));
        }

        UpdateUnread();
    }

    private void OnItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(NotificationViewModel.IsRead))
        {
            UpdateUnread();
        }
    }

    private void UpdateUnread() => UnreadCount = Items.Count(i => !i.IsRead);
}
