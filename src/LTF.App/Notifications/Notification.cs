using LTF.App.Navigation;

namespace LTF.App.Notifications;

/// <summary>
/// One inbox item. From M21 these are fed by real career events (a dated news feed).
/// <see cref="DeepLink"/> is the screen the item jumps to when opened (null = informational only).
/// <see cref="Date"/> is the career date the item was raised; <see cref="RequiresAction"/> marks an item
/// that pauses Continue until the player acts (ROADMAP Rev 15). Both are optional so the M19 sample source
/// and other callers keep constructing items positionally.
/// </summary>
public sealed record Notification(
    string Id,
    NotificationCategory Category,
    NotificationSeverity Severity,
    string Title,
    string Body,
    NavKey? DeepLink,
    DateOnly Date = default,
    bool RequiresAction = false);
