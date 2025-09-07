using OrderService.Application.Services;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Shared.Kernel.Application.Models;

namespace OrderService.Infrastructure.Services;

public class RabbitMQEventPublisher : IEventPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly string _exchangeName;
    private readonly ILogger <RabbitMQEventPublisher> _logger;
    public RabbitMQEventPublisher(ILogger<RabbitMQEventPublisher> logger, string connectionString, string exchangeName = "order-events")
    {
        _logger = logger;
        var factory = new ConnectionFactory() { Uri = new Uri(connectionString) };
        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
        _exchangeName = exchangeName;

        
        _channel.ExchangeDeclareAsync(exchange: _exchangeName, type: ExchangeType.Topic, durable: true).GetAwaiter().GetResult();
    }

    public async Task PublishAsync(string eventType, string eventData, string routingKey = "")
    {
        try
        {
            if (string.IsNullOrEmpty(routingKey))
                routingKey = eventType.ToLowerInvariant();
        
            var message = new RabbitMqPublishEventModel(Guid.NewGuid(), eventType, eventData);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

            var properties = new BasicProperties
            {
                Persistent = true,
                MessageId = message.Id.ToString(),
                Type = eventType,
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };

            await _channel.BasicPublishAsync(
                exchange: _exchangeName,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: properties,
                body: body
            );
        
            _logger.LogInformation("Event published: {EventType} with routing key: {RoutingKey}", 
                eventType, routingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event: {EventType}", eventType);
            throw;
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
