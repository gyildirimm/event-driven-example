namespace Shared.Kernel.Application.EventModels.Order;

public class OrderConfirmedEvent
{
    public Guid OrderId { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public DateTime OccurredOn { get; set; }
    public List<OrderLineConfirmedEvent> OrderLines { get; set; } = new();
}

public class OrderLineConfirmedEvent
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}