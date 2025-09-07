namespace Shared.Kernel.Application.Models;

public sealed class RabbitMqPublishEventModel(Guid id, string type, string? data = null)
{
    public Guid Id { get; set; } = id;
    public string Type { get; set; } = type;
    public string Data { get; set; } = data;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}