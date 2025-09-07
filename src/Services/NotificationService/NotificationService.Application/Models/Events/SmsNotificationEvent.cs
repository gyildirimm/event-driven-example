using NotificationService.Domain.Enums;

namespace NotificationService.Application.Models.Events;

public record SmsNotificationEvent(
    Guid NotificationId,
    string Recipient,
    string Subject,
    string Content,
    Dictionary<string, object>? Metadata = null
) : INotificationEvent
{
    public NotificationType Type => NotificationType.Sms;
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid EventId { get; } = Guid.NewGuid();
}