using System.Collections.Generic;
using LTF.App.Navigation;
using LTF.App.Notifications;

namespace LTF.App.Services;

/// <summary>A fixed set of illustrative notifications for M19 (fictional, ADR-0003). Mirrors the mockup's
/// categories and shows the deep-link + severity flow before the engine feeds real events (M21).</summary>
public sealed class SampleNotificationSource : INotificationSource
{
    public IReadOnlyList<Notification> Current() => new[]
    {
        new Notification("board-1", NotificationCategory.Board, NotificationSeverity.Warning,
            "Board expects a points finish", "The board wants both cars inside the top ten this weekend.",
            NavKey.BoardSponsors),
        new Notification("finance-1", NotificationCategory.Finance, NotificationSeverity.Critical,
            "Cost cap headroom is tight", "Only a sliver of cap room remains for the rest of the season.",
            NavKey.Finance),
        new Notification("rnd-1", NotificationCategory.RnD, NotificationSeverity.Success,
            "Front wing approved for race", "The new front wing cleared its track test and is fitted.",
            NavKey.RndFacilities),
        new Notification("driver-1", NotificationCategory.Staff, NotificationSeverity.Info,
            "Contract talks opening", "Your lead driver's deal expires at the end of the season.",
            NavKey.Drivers),
        new Notification("reg-1", NotificationCategory.Regulation, NotificationSeverity.Info,
            "Regulation vote scheduled", "A floor-height change goes to a vote next month.",
            null),
    };
}
