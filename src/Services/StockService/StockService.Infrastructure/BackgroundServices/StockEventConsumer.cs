using StockService.Application.Services;
using StockService.Infrastructure.EventHandlers;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Shared.Kernel.Application.EventModels.Order;
using Shared.Kernel.Application.EventModels.Stock;
using Shared.Kernel.Application.Models;

namespace StockService.Infrastructure.BackgroundServices;

public class StockEventConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StockEventConsumer> _logger;
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly string _queueName = "stock-events-queue";
    private readonly string _exchangeName = "stock-events";

    public StockEventConsumer(IServiceProvider serviceProvider, ILogger<StockEventConsumer> logger, string connectionString)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        var factory = new ConnectionFactory() { Uri = new Uri(connectionString) };
        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        // Exchange'leri declare et
        _channel.ExchangeDeclareAsync(exchange: _exchangeName, type: ExchangeType.Topic, durable: true).GetAwaiter().GetResult();
        _channel.ExchangeDeclareAsync(exchange: "order-events", type: ExchangeType.Topic, durable: true).GetAwaiter().GetResult();

        // Main queue'yu basit şekilde declare et
        _channel.QueueDeclareAsync(queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null).GetAwaiter().GetResult();
            
        _logger.LogInformation("Main queue {QueueName} declared", _queueName);
        
        // Event'leri bind et
        _channel.QueueBindAsync(queue: _queueName, exchange: _exchangeName, routingKey: "stockreservationrequested").GetAwaiter().GetResult();
        _channel.QueueBindAsync(queue: _queueName, exchange: "order-events", routingKey: "orderconfirmed").GetAwaiter().GetResult();

        _logger.LogInformation("RabbitMQ connection initialized for Stock Event Consumer");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Stock Event Consumer started");

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnEventReceived;

        await _channel.BasicConsumeAsync(
            queue: _queueName,
            autoAck: false,
            consumer: consumer, cancellationToken: stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task OnEventReceived(object sender, BasicDeliverEventArgs eventArgs)
    {
        var routingKey = eventArgs.RoutingKey;
        var body = eventArgs.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);

        try
        {
            _logger.LogInformation("Raw message received: {Message}", message);
            
            using var scope = _serviceProvider.CreateScope();
            var stockEventHandler = scope.ServiceProvider.GetRequiredService<StockEventHandler>();
            
            RabbitMqPublishEventModel? eventMessage = JsonSerializer.Deserialize<RabbitMqPublishEventModel>(message);

            if (eventMessage != null)
            {
                _logger.LogInformation("Received event: {EventType}", eventMessage.Type);

                switch (eventMessage.Type.ToLowerInvariant())
                {
                    case "stockreservationrequested":
                        var stockReservationRequestedEvent = JsonSerializer.Deserialize<StockReservationRequestedEvent>(eventMessage.Data!);
                        if (stockReservationRequestedEvent != null)
                            await stockEventHandler.HandleStockReservationRequestedAsync(stockReservationRequestedEvent);
                        else
                            throw new ArgumentNullException(nameof(stockReservationRequestedEvent));
                        break;
                    case "orderconfirmed":
                        var orderConfirmedEvent = JsonSerializer.Deserialize<OrderConfirmedEvent>(eventMessage.Data!);
                        if (orderConfirmedEvent != null)
                            await stockEventHandler.HandleOrderConfirmedAsync(orderConfirmedEvent);
                        else
                            throw new ArgumentNullException(nameof(orderConfirmedEvent));
                        break;
                    default:
                        _logger.LogWarning("Unknown event type: {EventType}", eventMessage.Type);
                        break;
                }

                await _channel.BasicAckAsync(eventArgs.DeliveryTag, false);
                _logger.LogInformation("Successfully processed event with routing key: {RoutingKey}", routingKey);
            }
            else
            {
                throw new ArgumentNullException(nameof(eventMessage));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing stock event with routing key: {RoutingKey}, Message: {Message}", routingKey, message);
            
            // Basit NACK - mesajı reddet ve requeue yap
            await _channel.BasicNackAsync(eventArgs.DeliveryTag, false, true);
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}