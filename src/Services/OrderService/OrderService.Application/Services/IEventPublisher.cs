namespace OrderService.Application.Services;

public interface IEventPublisher
{
    Task PublishAsync(string eventType, string eventData, string routingKey = "");
}
