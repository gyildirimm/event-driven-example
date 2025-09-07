using OrderService.Domain.Enums;
using Shared.Kernel.Domain.DDD;
using OrderService.Domain.ValueObjects;
using System.Text.Json;
using Shared.Kernel.Application.EventModels.Order;

namespace OrderService.Domain.Entities;

public sealed class Order : AggregateRoot
{
    public string CustomerId { get; private set; } = string.Empty;
    public string CustomerEmail { get; private set; } = string.Empty;
    public OrderStatus Status { get; private set; }
    public Money TotalAmount { get; private set; } = Money.Zero();
    public string? Notes { get; private set; }
    
    public List<OrderLine> OrderLines { get; private set; } = new();
    public List<OutboxEvent> OutboxEvents { get; private set; } = new();

    private Order() : base()
    {
    }

    public Order(string customerId, string customerEmail, string? notes = null) : base()
    {
        CustomerId = customerId ?? throw new ArgumentNullException(nameof(customerId));
        CustomerEmail = customerEmail ?? throw new ArgumentNullException(nameof(customerEmail));
        Status = OrderStatus.Pending;
        Notes = notes;
        TotalAmount = Money.Zero();
    }

    public void AddOrderLine(string productId, string productName, Money unitPrice, int quantity)
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Cannot add items to a non-pending order");

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

        var existingLine = OrderLines.FirstOrDefault(ol => ol.ProductId == productId);
        if (existingLine != null)
        {
            existingLine.UpdateQuantity(existingLine.Quantity + quantity);
        }
        else
        {
            var orderLine = new OrderLine(productId, productName, unitPrice, quantity);
            orderLine.SetOrderId(Id);
            orderLine.SetOrder(this);
            OrderLines.Add(orderLine);
        }

        RecalculateTotalAmount();
    }

    // Backward compatibility method with decimal
    public void AddOrderLine(string productId, string productName, decimal unitPrice, int quantity)
    {
        AddOrderLine(productId, productName, new Money(unitPrice), quantity);
    }

    public void RequestStockReservation()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Can only request stock reservation for pending orders");

        if (!OrderLines.Any())
            throw new InvalidOperationException("Cannot request stock reservation for an order without items");

        Status = OrderStatus.AwaitingStockReservation;
        
        var eventData = new
        {
            OrderId = Id,
            Items = OrderLines.Select(ol => new
            {
                ProductId = ol.ProductId,
                Quantity = ol.Quantity
            }).ToArray(),
            OccurredOn = DateTime.UtcNow
        };

        var outboxEvent = new OutboxEvent(
            "StockReservationRequested",
            JsonSerializer.Serialize(eventData),
            "stock-events"
        );

        OutboxEvents.Add(outboxEvent);
        SetUpdatedAt();
    }

    public void MarkStockReserved()
    {
        if (Status != OrderStatus.AwaitingStockReservation)
            throw new InvalidOperationException("Can only mark stock as reserved for orders awaiting stock reservation");

        Status = OrderStatus.StockReserved;
        SetUpdatedAt();
    }

    public void MarkStockReservationFailed(string reason = "Stock not available")
    {
        if (Status != OrderStatus.AwaitingStockReservation)
            throw new InvalidOperationException("Can only mark stock reservation as failed for orders awaiting stock reservation");

        Status = OrderStatus.StockReservationFailed;
        Notes = $"{Notes} - Stock reservation failed: {reason}".Trim(' ', '-');
        SetUpdatedAt();
    }

    public void RemoveOrderLine(string productId)
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Cannot remove items from a non-pending order");

        var orderLine = OrderLines.FirstOrDefault(ol => ol.ProductId == productId);
        if (orderLine != null)
        {
            OrderLines.Remove(orderLine);
            RecalculateTotalAmount();
        }
    }

    public void UpdateOrderLineQuantity(string productId, int newQuantity)
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Cannot update quantities in a non-pending order");

        if (newQuantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(newQuantity));

        var orderLine = OrderLines.FirstOrDefault(ol => ol.ProductId == productId);
        if (orderLine == null)
            throw new InvalidOperationException($"Order line with product ID {productId} not found");

        orderLine.UpdateQuantity(newQuantity);
        RecalculateTotalAmount();
    }

    public void ConfirmOrder()
    {
        if (Status != OrderStatus.StockReserved)
            throw new InvalidOperationException("Only orders with reserved stock can be confirmed");

        if (!OrderLines.Any())
            throw new InvalidOperationException("Cannot confirm an order without order lines");

        Status = OrderStatus.Confirmed;
        SetUpdatedAt();
        
        var orderConfirmedEvent = new OrderConfirmedEvent
        {
            OrderId = Id,
            CustomerId = CustomerId,
            CustomerEmail = CustomerEmail,
            OrderLines = OrderLines.Select(ol => new OrderLineConfirmedEvent
            {
                ProductId = Guid.Parse(ol.ProductId),
                Quantity = ol.Quantity,
            }).ToList(),
        };
        
        var outboxEvent = new OutboxEvent(
            "OrderConfirmed",
            JsonSerializer.Serialize(orderConfirmedEvent),
            "order-events"
        );

        OutboxEvents.Add(outboxEvent);
    }

    public void CancelOrder()
    {
        if (Status == OrderStatus.Shipped || Status == OrderStatus.Delivered)
            throw new InvalidOperationException("Cannot cancel a shipped or delivered order");

        Status = OrderStatus.Cancelled;
        SetUpdatedAt();
    }

    public void MarkAsShipped()
    {
        if (Status != OrderStatus.Confirmed)
            throw new InvalidOperationException("Only confirmed orders can be marked as shipped");

        Status = OrderStatus.Shipped;
        SetUpdatedAt();
        
    }

    public void MarkAsDelivered()
    {
        if (Status != OrderStatus.Shipped)
            throw new InvalidOperationException("Only shipped orders can be marked as delivered");

        Status = OrderStatus.Delivered;
        SetUpdatedAt();
    }

    private void RecalculateTotalAmount()
    {
        TotalAmount = OrderLines.Aggregate(Money.Zero(), (sum, ol) => sum + ol.TotalPrice);
        SetUpdatedAt();
    }
}