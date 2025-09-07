using NotificationService.Domain.Enums;

namespace NotificationService.Application.Models;

public class NotificationQueryParameters
{
    public string? Recipient { get; set; }
    public NotificationStatus? Status { get; set; }
    public NotificationType? Channel { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}