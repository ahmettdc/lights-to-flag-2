using System;
using System.Collections.Generic;
using LTF.App.Notifications;

namespace LTF.App.Services;

/// <summary>
/// The live career's notification feed (M21): an appendable, in-order list the career pushes dated items
/// into as it advances. Replaces the M19 <see cref="SampleNotificationSource"/> in the running app; items
/// arrive in career-date order (Continue advances chronologically), so no re-sorting is needed.
/// </summary>
public sealed class CareerNotificationSource : IMutableNotificationSource
{
    private readonly List<Notification> _items = new();

    public IReadOnlyList<Notification> Current() => _items;

    public event Action? Changed;

    public void Reset()
    {
        _items.Clear();
        Changed?.Invoke();
    }

    public void Append(Notification notification)
    {
        _items.Add(notification);
        Changed?.Invoke();
    }
}
