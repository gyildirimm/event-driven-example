using StockService.Application.Services;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Shared.Kernel.Application.EventModels.Order;
using Shared.Kernel.Application.EventModels.Stock;

namespace StockService.Infrastructure.EventHandlers;

public class StockEventHandler(
    IStockService stockService,
    IEventPublisher eventPublisher,
    ILogger<StockEventHandler> logger)
{
    public async Task HandleStockReservationRequestedAsync(StockReservationRequestedEvent stockReservationRequestedEvent)
    {
        try
        {
            // var stockReservationRequestedEvent = JsonSerializer.Deserialize<StockReservationRequestedEvent>(eventData);
            if (stockReservationRequestedEvent == null)
            {
                logger.LogWarning("Failed to deserialize StockReservationRequested event");
                return;
            }

            logger.LogInformation("Processing stock reservation request for order {OrderId}", stockReservationRequestedEvent.OrderId);

            var allItemsReserved = true;
            var reservationResults = new List<StockReservationResult>();

            // Her bir ürün için stok rezervasyonu yap
            foreach (var item in stockReservationRequestedEvent.Items)
            {
                var productId = item.ProductId;
                var reserveRequest = new StockService.Application.DTOs.ReserveStockRequest
                {
                    ProductId = productId,
                    Quantity = item.Quantity,
                    OrderId = stockReservationRequestedEvent.OrderId
                };

                var result = await stockService.ReserveStockAsync(reserveRequest);
                
                if (result.IsSuccessful)
                {
                    logger.LogInformation("Successfully reserved {Quantity} units of product {ProductId} for order {OrderId}", 
                        item.Quantity, productId, stockReservationRequestedEvent.OrderId);
                    
                    reservationResults.Add(new StockReservationResult
                    {
                        ProductId = item.ProductId.ToString(),
                        Quantity = item.Quantity,
                        IsSuccess = true,
                        ReservedQuantity = item.Quantity
                    });
                }
                else
                {
                    logger.LogWarning("Failed to reserve {Quantity} units of product {ProductId} for order {OrderId}: {Error}", 
                        item.Quantity, productId, stockReservationRequestedEvent.OrderId, result.Message);
                    
                    allItemsReserved = false;
                    reservationResults.Add(new StockReservationResult
                    {
                        ProductId = item.ProductId.ToString(),
                        Quantity = item.Quantity,
                        IsSuccess = false,
                        FailureReason = result.Message
                    });
                }
            }

            if (allItemsReserved)
            {
                await PublishStockReservedEventAsync(stockReservationRequestedEvent.OrderId, reservationResults);
            }
            else
            {
                await ReleaseSuccessfulReservationsAsync(stockReservationRequestedEvent.OrderId, reservationResults);
                await PublishStockReservationFailedEventAsync(stockReservationRequestedEvent.OrderId, reservationResults);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling StockReservationRequested event");
            throw;
        }
    }

    private async Task PublishStockReservedEventAsync(Guid orderId, List<StockReservationResult> reservationResults)
    {
        var eventData = new
        {
            OrderId = orderId,
            Items = reservationResults.Where(r => r.IsSuccess).Select(r => new
            {
                ProductId = r.ProductId,
                Quantity = r.Quantity,
                ReservedQuantity = r.ReservedQuantity
            }).ToArray(),
            OccurredOn = DateTime.UtcNow
        };

        await eventPublisher.PublishAsync("StockReserved", JsonSerializer.Serialize(eventData));
        logger.LogInformation("Published StockReserved event for order {OrderId}", orderId);
    }

    private async Task PublishStockReservationFailedEventAsync(Guid orderId, List<StockReservationResult> reservationResults)
    {
        var failedItems = reservationResults.Where(r => !r.IsSuccess).ToList();
        var reason = string.Join("; ", failedItems.Select(f => $"{f.ProductId}: {f.FailureReason}"));

        var eventData = new
        {
            OrderId = orderId,
            Reason = reason,
            Items = reservationResults.Select(r => new
            {
                ProductId = r.ProductId,
                Quantity = r.Quantity,
                IsSuccess = r.IsSuccess,
                FailureReason = r.FailureReason
            }).ToArray(),
            OccurredOn = DateTime.UtcNow
        };

        await eventPublisher.PublishAsync("StockReservationFailed", JsonSerializer.Serialize(eventData));
        logger.LogInformation("Published StockReservationFailed event for order {OrderId}", orderId);
    }

    public async Task HandleOrderConfirmedAsync(OrderConfirmedEvent orderConfirmedEvent)
    {
        try
        {
            if (orderConfirmedEvent == null)
            {
                logger.LogWarning("Failed to deserialize OrderConfirmed event");
                return;
            }

            logger.LogInformation("Processing order confirmation for order {OrderId}", orderConfirmedEvent.OrderId);

            foreach (var item in orderConfirmedEvent.OrderLines)
            {
                var productId = item.ProductId;
                var result = await stockService.ConfirmReservationAsync(productId, orderConfirmedEvent.OrderId, item.Quantity);
                
                if (result.IsSuccessful)
                {
                    logger.LogInformation("Successfully confirmed reservation and deducted {Quantity} units of product {ProductId} for order {OrderId}", 
                        item.Quantity, productId, orderConfirmedEvent.OrderId);
                }
                else
                {
                    logger.LogError("Failed to confirm reservation for product {ProductId}, order {OrderId}: {Error}", 
                        productId, orderConfirmedEvent.OrderId, result.Message);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling OrderConfirmed event");
            throw;
        }
    }

    private async Task ReleaseSuccessfulReservationsAsync(Guid orderId, List<StockReservationResult> reservationResults)
    {
        var successfulReservations = reservationResults.Where(r => r.IsSuccess).ToList();
        
        foreach (var reservation in successfulReservations)
        {
            try
            {
                var productId = Guid.Parse(reservation.ProductId);
                await stockService.ReleaseReservationAsync(productId, orderId, reservation.Quantity);
                logger.LogInformation("Released reservation for product {ProductId}, order {OrderId}", productId, orderId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to release reservation for product {ProductId}, order {OrderId}", reservation.ProductId, orderId);
            }
        }
    }
}

public class StockReservationResult
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public bool IsSuccess { get; set; }
    public int ReservedQuantity { get; set; }
    public string? FailureReason { get; set; }
}
