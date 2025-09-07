using NotificationService.Domain.Enums;
using Shared.Kernel.Domain;

namespace NotificationService.Domain.Entities;

public sealed class Notification : Entity
{
    public NotificationType Channel { get; private set; }
    public NotificationStatus Status { get; private set; } = NotificationStatus.Created;

    public string Recipient { get; private set; } = null!;
    public string? Subject { get; private set; }
    public string? Body { get; private set; }
    public string? Text { get; private set; }

    public int AttemptCount { get; private set; }
    public string? LastError { get; private set; }
    public DateTime? SentAtUtc { get; private set; }

    private Notification() : base()
    {
        
    }

    private Notification(NotificationType channel, string recipient, string? subject, string? body, string? text) : base()
    {
        Channel = channel;
        Recipient = recipient;
        Subject = subject;
        Body = body;
        Text = text;
    }

    public static Notification Email(string to, string subject, string body)
        => new(NotificationType.Email, to, subject, body, null);

    public static Notification Sms(string to, string text)
        => new(NotificationType.Sms, to, null, null, text);

    public void MarkCreated()
    {
        Status = NotificationStatus.Created;
        SetUpdatedAt();
    }
    
    public void MarkQueued()
    {
        Status = NotificationStatus.Queued;
        SetUpdatedAt();
    }
    
    public void MarkDelivered()
    {
        SentAtUtc = DateTime.UtcNow;
        Status = NotificationStatus.Delivered;
        SetUpdatedAt();
    }

    public void MarkUndeliverable(string e)
    {
        LastError = e; 
        Status = NotificationStatus.Undeliverable;
        SetUpdatedAt();
    }

    public void MarkFailed(string e)
    {
        LastError = e; 
        AttemptCount++; 
        Status = NotificationStatus.Failed;
        SetUpdatedAt();
    }

    public void IncrementAttempt()
    {
        AttemptCount++;
        SetUpdatedAt();
    }
}