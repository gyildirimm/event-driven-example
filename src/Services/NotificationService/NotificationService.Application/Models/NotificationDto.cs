using NotificationService.Domain.Enums;

namespace NotificationService.Application.Models;

public class NotificationDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public NotificationType Channel { get; set; }
    public NotificationStatus Status { get; set; }

    public string Recipient { get; set; }
    public string? Subject { get; set; }
    public string? Body { get;  set; }
    public string? Text { get; set; }

    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
    public DateTime? SentAtUtc { get; set; }
}