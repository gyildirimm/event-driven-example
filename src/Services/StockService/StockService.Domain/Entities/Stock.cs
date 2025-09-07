using Shared.Kernel.Domain.DDD;

namespace StockService.Domain.Entities;

public class Stock : AggregateRoot
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public int ReservedQuantity { get; set; }
    
    public int AvailableQuantity => Quantity - ReservedQuantity;

    public Stock()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    public Stock(Guid productId, int quantity)
    {
        Id = Guid.NewGuid();
        ProductId = productId;
        Quantity = quantity;
        ReservedQuantity = 0;
        CreatedAt = DateTime.UtcNow;
    }

    public bool CanReserve(int quantity)
    {
        return AvailableQuantity >= quantity;
    }

    public bool ReserveStock(int quantity)
    {
        if (!CanReserve(quantity))
            return false;

        ReservedQuantity += quantity;
        UpdatedAt = DateTime.UtcNow;
        return true;
    }

    public bool ReleaseReservation(int quantity)
    {
        if (ReservedQuantity < quantity)
            return false;

        ReservedQuantity -= quantity;
        UpdatedAt = DateTime.UtcNow;
        return true;
    }

    public bool ConfirmReservation(int quantity)
    {
        if (ReservedQuantity < quantity)
            return false;

        // Stoktan düş ve rezervasyonu azalt
        Quantity -= quantity;
        ReservedQuantity -= quantity;
        UpdatedAt = DateTime.UtcNow;
        return true;
    }

    public void UpdateQuantity(int newQuantity)
    {
        if (newQuantity < ReservedQuantity)
            throw new InvalidOperationException("Cannot set quantity below reserved amount");

        Quantity = newQuantity;
        UpdatedAt = DateTime.UtcNow;
    }
}
