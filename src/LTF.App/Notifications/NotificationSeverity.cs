namespace LTF.App.Notifications;

/// <summary>How urgent a notification is, driving its dot colour and (later) whether it halts Continue.</summary>
public enum NotificationSeverity
{
    Info,
    Success,
    Warning,
    Critical,
}
