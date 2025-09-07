namespace NotificationService.Application.Models;

public class NotificationSmsCreateModel
{
    public string Recipient { get; set; } = null!;
    public string Text { get; set; } = null!;
}

public class NotificationEmailCreateModel
{
    public string Recipient { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string Body { get; set; } = null!;
}