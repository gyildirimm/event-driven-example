using Shared.Kernel.Domain;
using OrderService.Domain.ValueObjects;

namespace OrderService.Domain.Entities;

public sealed class OrderLine : Entity
{
    public string ProductId { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;
    public Money UnitPrice { get; private set; } = Money.Zero();
    public int Quantity { get; private set; }
    public Money TotalPrice => UnitPrice * Quantity;
    
    public Guid OrderId { get; private set; }
    public Order Order { get; private set; } = null!;
    private OrderLine() : base()
    {
    }

    public OrderLine(string productId, string productName, Money unitPrice, int quantity) : base()
    {
        ProductId = productId ?? throw new ArgumentNullException(nameof(productId));
        ProductName = productName ?? throw new ArgumentNullException(nameof(productName));
        UnitPrice = unitPrice ?? throw new ArgumentNullException(nameof(unitPrice));
        
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

        Quantity = quantity;
    }

    internal void SetOrder(Order order)
    {
        Order = order ?? throw new ArgumentNullException(nameof(order));
        OrderId = order.Id;
    }
    
    // Backward compatibility constructor with decimal
    public OrderLine(string productId, string productName, decimal unitPrice, int quantity) 
        : this(productId, productName, new Money(unitPrice), quantity)
    {
    }

    public void UpdateQuantity(int newQuantity)
    {
        if (newQuantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(newQuantity));

        Quantity = newQuantity;
        SetUpdatedAt();
    }

    public void UpdateUnitPrice(Money newUnitPrice)
    {
        UnitPrice = newUnitPrice ?? throw new ArgumentNullException(nameof(newUnitPrice));
        SetUpdatedAt();
    }

    // Backward compatibility method with decimal
    public void UpdateUnitPrice(decimal newUnitPrice)
    {
        UpdateUnitPrice(new Money(newUnitPrice));
    }
    
    internal void SetOrderId(Guid orderId)
    {
        OrderId = orderId;
    }
}