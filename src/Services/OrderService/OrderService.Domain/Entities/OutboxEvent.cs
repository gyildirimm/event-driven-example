using Shared.Kernel.Domain;
using Shared.Kernel.Domain.DDD;

namespace OrderService.Domain.Entities;

public class OutboxEvent : Entity
{
    public string Type { get; private set; } = string.Empty;
    public string Data { get; private set; } = string.Empty;

    public string ExchangeName { get; set; }
    public DateTime OccurredOn { get; private set; }
    public bool Processed { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? Error { get; private set; }
    public int RetryCount { get; private set; }
    public int MaxRetries { get; private set; } = 3;

    private OutboxEvent() : base() { }

    public OutboxEvent(string type, string data, string exchangeName, int maxRetries = 3) : base()
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Data = data ?? throw new ArgumentNullException(nameof(data));
        OccurredOn = DateTime.UtcNow;
        Processed = false;
        RetryCount = 0;
        MaxRetries = maxRetries;
        ExchangeName = exchangeName ?? throw new ArgumentNullException(nameof(exchangeName));
    }

    public void MarkAsProcessed()
    {
        Processed = true;
        ProcessedAt = DateTime.UtcNow;
        Error = null;
        SetUpdatedAt();
    }

    public void MarkAsFailed(string error)
    {
        Error = error;
        RetryCount++;
        SetUpdatedAt();
    }

    public bool CanRetry()
    {
        return RetryCount < MaxRetries;
    }

    public bool HasExceededMaxRetries()
    {
        return RetryCount >= MaxRetries;
    }
}
