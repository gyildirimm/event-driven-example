using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Models.Events;
using NotificationService.Application.Services;
using NotificationService.Domain.Enums;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Shared.Kernel.Application.OperationResults;

namespace NotificationService.Infrastructure.BackgroundServices;

public class SmsConsumer : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SmsConsumer> _logger;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly string _exchangeName = "notification-events";
    private readonly string _queueName = "notification-service-sms";

    public SmsConsumer(IServiceProvider serviceProvider, ILogger<SmsConsumer> logger, string connectionString)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        var factory = new ConnectionFactory() { Uri = new Uri(connectionString) };
        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

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

        _channel.QueueBindAsync(queue: _queueName,
            exchange: _exchangeName,
            routingKey: "notification.sms").GetAwaiter().GetResult();

        _logger.LogInformation("RabbitMQ connection initialized for Sms Sender Listener");
    }
    
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SMS Event Listener starting...");
        
        try
        {
            await base.StartAsync(cancellationToken);
            _logger.LogInformation("SMS Event Listener started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start SMS Event Listener");
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

        _logger.LogInformation("SMS Event Listener is listening for events...");

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
            INotificationSenderService smsSender = scope.ServiceProvider.GetRequiredKeyedService<INotificationSenderService>("sms-sender");
            IEventPublisher smsPublisher = scope.ServiceProvider.GetRequiredKeyedService<IEventPublisher>("notification-events");
            INotificationService notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            if (routingKey == "notification.sms")
            {
                SmsNotificationEvent? model = ConvertSmsModel(message);
                if (model != null)
                {
                    OperationResult result = await smsSender.SendNotificationAsync(model!);
                    if(!result.IsSuccessful)
                        await smsPublisher.PublishAsync("notification.sms.failed", message, "notification.sms.failed");
                }
                else
                    throw new ArgumentNullException(nameof(SmsNotificationEvent));
            }
            else if (routingKey == "notification.sms.failed")
            {
                SmsNotificationEvent? model = ConvertSmsModel(message);
                if (model != null)
                {
                    OperationResult result =
                        await notificationService.UpdateNotificationStatusAsync(model.NotificationId,
                            NotificationStatus.Failed);

                    if (!result.IsSuccessful)
                        throw new Exception("Failed to update notification status to Failed");
                }
                else
                    throw new ArgumentNullException(nameof(SmsNotificationEvent));
            }
            else
            {
                _logger.LogWarning("Unknown event type received: {RoutingKey}", routingKey);
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
        _logger.LogInformation("SMS Event Listener stopping...");
        
        if (_channel != null)
            await _channel.CloseAsync(cancellationToken: cancellationToken);
        if (_connection != null)
            await _connection.CloseAsync(cancellationToken: cancellationToken);
        
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("SMS Event Listener stopped");
    }
    
    private SmsNotificationEvent? ConvertSmsModel(string eventData)
    {
        var wrapperMessage = JsonSerializer.Deserialize<EventWrapper>(eventData);
        if (wrapperMessage?.Data == null)
        {
            _logger.LogWarning("Failed to deserialize wrapper message or Data is null");
            return null;
        }
        
        var smsModel = JsonSerializer.Deserialize<SmsNotificationEvent>(wrapperMessage.Data);
        if (smsModel == null)
        {
            _logger.LogWarning("Failed to deserialize SmsNotificationEvent event from Data property");
            return null;
        }
        
        return smsModel;
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}