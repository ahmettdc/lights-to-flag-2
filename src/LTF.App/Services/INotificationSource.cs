using System.Collections.Generic;
using LTF.App.Notifications;

namespace LTF.App.Services;

/// <summary>Supplies the inbox's notifications. M19 uses a sample source; M21 swaps in an engine-backed
/// one that pushes real events — with no change to the inbox view-model or view.</summary>
public interface INotificationSource
{
    IReadOnlyList<Notification> Current();
}
