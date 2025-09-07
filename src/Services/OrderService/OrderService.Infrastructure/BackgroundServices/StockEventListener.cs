using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderService.Infrastructure.EventHandlers;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace OrderService.Infrastructure.BackgroundServices;

public class StockEventListener : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StockEventListener> _logger;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly string _exchangeName = "stock-events";
    private readonly string _queueName = "order_service_stock_events";

    public StockEventListener(IServiceProvider serviceProvider, ILogger<StockEventListener> logger, string connectionString)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        var factory = new ConnectionFactory() { Uri = new Uri(connectionString) };
        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
        
        // Exchange'i declare et
        _channel.ExchangeDeclareAsync(exchange: _exchangeName,
            type: ExchangeType.Topic,
            durable: true).GetAwaiter().GetResult();

        // Main queue'yu basit şekilde declare et
        _channel.QueueDeclareAsync(queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null).GetAwaiter().GetResult();
            
        _logger.LogInformation("Main queue {QueueName} declared", _queueName);

        // Queue'yu exchange'e bind et
        _channel.QueueBindAsync(queue: _queueName,
            exchange: _exchangeName,
            routingKey: "stockreserved").GetAwaiter().GetResult();

        _channel.QueueBindAsync(queue: _queueName,
            exchange: _exchangeName,
            routingKey: "stockreservationfailed").GetAwaiter().GetResult();

        _logger.LogInformation("RabbitMQ connection initialized for Stock Event Listener");
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stock Event Listener starting...");
        
        try
        {
            await base.StartAsync(cancellationToken);
            _logger.LogInformation("Stock Event Listener started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Stock Event Listener");
            throw;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        if (_channel == null)
        {
            _logger.LogError("Channel is not initialized");
            return;
        }

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnEventReceivedAsync;

        await _channel.BasicConsumeAsync(queue: _queueName,
                                       autoAck: false,
                                       consumer: consumer, cancellationToken: stoppingToken);

        _logger.LogInformation("Stock Event Listener is listening for events...");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task OnEventReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        var routingKey = ea.RoutingKey;
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);

        try
        {
            _logger.LogInformation("Received event with routing key: {RoutingKey}", routingKey);

            using var scope = _serviceProvider.CreateScope();
            var stockEventHandler = scope.ServiceProvider.GetRequiredService<StockEventHandler>();

            switch (routingKey)
            {
                case "stockreserved":
                    await stockEventHandler.HandleStockReservedAsync(message);
                    break;
                case "stockreservationfailed":
                    await stockEventHandler.HandleStockReservationFailedAsync(message);
                    break;
                default:
                    _logger.LogWarning("Unknown event type received: {RoutingKey}", routingKey);
                    break;
            }

            if (_channel != null)
                await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
            
            _logger.LogInformation("Successfully processed event with routing key: {RoutingKey}", routingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing event with routing key: {RoutingKey}, Message: {Message}", routingKey, message);
            
            // Basit NACK - mesajı reddet ve requeue yap
            if (_channel != null)
                await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
        }
    }
    
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stock Event Listener stopping...");
        
        if (_channel != null)
            await _channel.CloseAsync(cancellationToken: cancellationToken);
        if (_connection != null)
            await _connection.CloseAsync(cancellationToken: cancellationToken);
        
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("Stock Event Listener stopped");
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
