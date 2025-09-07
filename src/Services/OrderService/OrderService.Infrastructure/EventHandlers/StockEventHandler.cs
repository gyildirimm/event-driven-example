using OrderService.Application.Services;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Kernel.Application.Repositories;

namespace OrderService.Infrastructure.EventHandlers;

public class StockEventHandler(IOrderService orderService, ILogger<StockEventHandler> logger, [FromKeyedServices("order-events")] IEventPublisher orderEventPublisher)
{
    public async Task HandleStockReservedAsync(string eventData)
    {
        try
        {
            logger.LogInformation("Received StockReserved event: {EventData}", eventData);
            
            var wrapperMessage = JsonSerializer.Deserialize<EventWrapper>(eventData);
            if (wrapperMessage?.Data == null)
            {
                logger.LogWarning("Failed to deserialize wrapper message or Data is null");
                return;
            }
            
            var stockReservedEvent = JsonSerializer.Deserialize<StockReservedEvent>(wrapperMessage.Data);
            if (stockReservedEvent == null)
            {
                logger.LogWarning("Failed to deserialize StockReserved event from Data property");
                return;
            }

            logger.LogInformation("Processing StockReserved event for order {OrderId}", stockReservedEvent.OrderId);

            // Stok rezervasyonu başarılı oldu, siparişi StockReserved durumuna geçir
            var result = await orderService.ConfirmStockReservationAsync(stockReservedEvent.OrderId);
            if (result.IsSuccessful)
            {
                logger.LogInformation("Successfully updated order {OrderId} status to StockReserved", stockReservedEvent.OrderId);
                
                var confirmResult = await orderService.ConfirmOrderAsync(stockReservedEvent.OrderId);
                if (confirmResult.IsSuccessful)
                {
                    logger.LogInformation("Successfully confirmed order {OrderId}", stockReservedEvent.OrderId);
                }
                else
                {
                    logger.LogError("Failed to confirm order {OrderId}: {Error}", stockReservedEvent.OrderId, confirmResult.Message);
                }
            }
            else
            {
                logger.LogError("Failed to update order {OrderId} status: {Error}", stockReservedEvent.OrderId, result.Message);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling StockReserved event for data: {EventData}", eventData);
            throw;
        }
    }

    public async Task HandleStockReservationFailedAsync(string eventData)
    {
        try
        {
            logger.LogInformation("Received StockReservationFailed event: {EventData}", eventData);
            
            var wrapperMessage = JsonSerializer.Deserialize<EventWrapper>(eventData);
            if (wrapperMessage?.Data == null)
            {
                logger.LogWarning("Failed to deserialize wrapper message or Data is null");
                return;
            }
            
            var stockReservationFailedEvent = JsonSerializer.Deserialize<StockReservationFailedEvent>(wrapperMessage.Data);
            if (stockReservationFailedEvent == null)
            {
                logger.LogWarning("Failed to deserialize StockReservationFailed event from Data property");
                return;
            }

            logger.LogInformation("Processing StockReservationFailed event for order {OrderId}", stockReservationFailedEvent.OrderId);

            var result = await orderService.FailStockReservationAsync(
                stockReservationFailedEvent.OrderId, 
                stockReservationFailedEvent.Reason);
                
            if (result.IsSuccessful)
            {
                logger.LogInformation("Successfully updated order {OrderId} status to StockReservationFailed with reason: {Reason}", 
                    stockReservationFailedEvent.OrderId, stockReservationFailedEvent.Reason);
            }
            else
            {
                logger.LogError("Failed to update order {OrderId} status: {Error}", 
                    stockReservationFailedEvent.OrderId, result.Message);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling StockReservationFailed event for data: {EventData}", eventData);
            throw;
        }
    }
}

public class EventWrapper
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class StockReservedEvent
{
    public Guid OrderId { get; set; }
    public List<StockReservedItem> Items { get; set; } = new();
    public DateTime OccurredOn { get; set; }
}

public class StockReservationFailedEvent
{
    public Guid OrderId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public List<StockReservationFailedItem> Items { get; set; } = new();
    public DateTime OccurredOn { get; set; }
}

public class StockReservedItem
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int ReservedQuantity { get; set; }
}

public class StockReservationFailedItem
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public bool IsSuccess { get; set; }
    public string? FailureReason { get; set; }
}
