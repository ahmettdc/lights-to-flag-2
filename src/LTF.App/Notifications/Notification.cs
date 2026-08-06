using LTF.App.Navigation;

namespace LTF.App.Notifications;

/// <summary>
/// One inbox item. In M19 these come from a sample source; M21 will feed them from real engine events.
/// <see cref="DeepLink"/> is the screen the item jumps to when opened (null = informational only).
/// </summary>
public sealed record Notification(
    string Id,
    NotificationCategory Category,
    NotificationSeverity Severity,
    string Title,
    string Body,
    NavKey? DeepLink);
