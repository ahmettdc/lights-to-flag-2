using System;
using System.Collections.Generic;
using LTF.App.Notifications;

namespace LTF.App.Services;

/// <summary>Supplies the inbox's notifications. M19 uses a sample source; M21 swaps in an engine-backed
/// one that pushes real events — with no change to the inbox view-model or view.</summary>
public interface INotificationSource
{
    IReadOnlyList<Notification> Current();
}

/// <summary>An appendable notification source (M21): the career pushes dated items as it advances, and
/// resets the feed when a new career opens. <see cref="Changed"/> lets the inbox track appended items.</summary>
public interface IMutableNotificationSource : INotificationSource
{
    /// <summary>Clear the feed (a new career starts fresh).</summary>
    void Reset();

    /// <summary>Append one item and notify subscribers.</summary>
    void Append(Notification notification);

    /// <summary>Raised after the feed changes (an append or a reset).</summary>
    event Action? Changed;
}
