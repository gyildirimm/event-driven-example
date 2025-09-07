using NotificationService.Domain.Enums;

namespace NotificationService.Application.Models.Events;

public record EmailNotificationEvent(
    Guid NotificationId,
    string Recipient,
    string Subject,
    string Content,
    Dictionary<string, object>? Metadata = null
) : INotificationEvent
{
    public NotificationType Type => NotificationType.Email;
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
    public Guid EventId { get; } = Guid.NewGuid();
}