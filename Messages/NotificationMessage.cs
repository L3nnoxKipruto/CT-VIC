namespace DataEntryApp.Messages;

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error
}

public record NotificationMessage(string Title, string Message, NotificationType Type = NotificationType.Info);
