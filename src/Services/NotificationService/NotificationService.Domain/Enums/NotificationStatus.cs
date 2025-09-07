namespace NotificationService.Domain.Enums;

public enum NotificationStatus
{
    Created,
    Queued,
    Delivered,
    Undeliverable,
    Failed
}