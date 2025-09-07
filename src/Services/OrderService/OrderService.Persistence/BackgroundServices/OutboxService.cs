using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderService.Application.Services;
using OrderService.Persistence.Contexts;

namespace OrderService.Persistence.BackgroundServices;

public class OutboxService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxService> _logger;
    private readonly TimeSpan _delay = TimeSpan.FromSeconds(30);

    public OutboxService(IServiceProvider serviceProvider, ILogger<OutboxService> logger)
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
        var context = scope.ServiceProvider.GetRequiredService<OrderContext>();
        var eventPublisher = scope.ServiceProvider.GetRequiredKeyedService<IEventPublisher>("stock-events");
        var oderPublisher = scope.ServiceProvider.GetRequiredKeyedService<IEventPublisher>("order-events");

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var unprocessedEvents = await context.OutboxEvents
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
                    case "stock-events":
                        publisher = eventPublisher;
                        break;
                    case "order-events":
                        publisher = oderPublisher;
                        break;
                    default:
                        _logger.LogWarning("Unknown exchange name {ExchangeName} for event {EventId}. Skipping.", 
                            outboxEvent.ExchangeName, outboxEvent.Id);
                        outboxEvent.MarkAsFailed("Unknown exchange name");
                        continue;
                }
                
                try
                {
                    await publisher.PublishAsync(outboxEvent.Type, outboxEvent.Data);
                    
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

            await context.SaveChangesAsync(cancellationToken);
        
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error in ProcessOutboxEventsAsync");
        }
    }
}
