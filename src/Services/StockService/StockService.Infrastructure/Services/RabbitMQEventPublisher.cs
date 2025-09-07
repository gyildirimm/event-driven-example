using StockService.Application.Services;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace StockService.Infrastructure.Services;

public class RabbitMQEventPublisher : IEventPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly string _exchangeName;

    public RabbitMQEventPublisher(string connectionString, string exchangeName = "stock-events")
    {
        var factory = new ConnectionFactory() { Uri = new Uri(connectionString) };
        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
        _exchangeName = exchangeName;
        
        _channel.ExchangeDeclareAsync(exchange: _exchangeName, type: ExchangeType.Topic, durable: true).GetAwaiter().GetResult();
    }

    public async Task PublishAsync(string eventType, string eventData, string routingKey = "")
    {
        if (string.IsNullOrEmpty(routingKey))
            routingKey = eventType.ToLowerInvariant();

        var message = new
        {
            Id = Guid.NewGuid(),
            Type = eventType,
            Data = eventData,
            Timestamp = DateTime.UtcNow
        };

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
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
