using NotificationService.Domain.Enums;

namespace NotificationService.Application.Models.Events;

public interface INotificationEvent
{
    Guid NotificationId { get; }
    string Recipient { get; }
    string Subject { get; }
    string Content { get; }
    NotificationType Type { get; }
    Dictionary<string, object>? Metadata { get; }
}