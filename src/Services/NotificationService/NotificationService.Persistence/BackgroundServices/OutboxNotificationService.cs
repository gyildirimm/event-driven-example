using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationService.Application.Repositories;
using NotificationService.Application.Services;
using NotificationService.Persistence.Contexts;
using Shared.Kernel.Application.Repositories;

namespace NotificationService.Persistence.BackgroundServices;

public class OutboxNotificationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxNotificationService> _logger;
    private readonly TimeSpan _delay = TimeSpan.FromSeconds(30);

    public OutboxNotificationService(IServiceProvider serviceProvider, ILogger<OutboxNotificationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxEventsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing outbox events.");
            }

            await Task.Delay(_delay, stoppingToken);
        }

        _logger.LogInformation("OutboxService stopped.");
    }

    private async Task ProcessOutboxEventsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();

        var eventPublisher = scope.ServiceProvider.GetRequiredKeyedService<IEventPublisher>("notification-events");
        IUnitOfWork<NotificationContext> _unitOfWork =
            scope.ServiceProvider.GetRequiredService<IUnitOfWork<NotificationContext>>();

        IOutboxNotificationEventRepository _outboxNotificationEventRepository =
            scope.ServiceProvider.GetRequiredService<IOutboxNotificationEventRepository>();
        
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var unprocessedEvents = await _outboxNotificationEventRepository.Query()
                .Where(e => !e.Processed && e.RetryCount < e.MaxRetries)
                .OrderBy(e => e.OccurredOn)
                .Take(10)
                .ToListAsync(cancellationToken);
            
            if (!unprocessedEvents.Any())
            {
                _logger.LogDebug("No unprocessed events found.");
                return;
            }

            _logger.LogInformation("Processing {Count} outbox events.", unprocessedEvents.Count);

            foreach (var outboxEvent in unprocessedEvents)
            {
                IEventPublisher? publisher = null;

                switch (outboxEvent.ExchangeName)
                {
                    case "notification-events":
                        publisher = eventPublisher;
                        break;
                    default:
                        _logger.LogWarning("Unknown exchange name {ExchangeName} for event {EventId}. Skipping.", 
                            outboxEvent.ExchangeName, outboxEvent.Id);
                        outboxEvent.MarkAsFailed("Unknown exchange name");
                        continue;
                }
                
                try
                {
                    await publisher.PublishAsync(outboxEvent.Type, outboxEvent.Data, routingKey: outboxEvent.Type);
                    
                    outboxEvent.MarkAsProcessed();
                    _logger.LogInformation("Successfully processed outbox event {EventId} of type {EventType}.", 
                        outboxEvent.Id, outboxEvent.Type);
                }
                catch (Exception ex)
                {
                    outboxEvent.MarkAsFailed(ex.Message);
                    _logger.LogError(ex, "Failed to process outbox event {EventId} of type {EventType}. Retry count: {RetryCount}", 
                        outboxEvent.Id, outboxEvent.Type, outboxEvent.RetryCount);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error in ProcessOutboxEventsAsync");
        }
    }
}