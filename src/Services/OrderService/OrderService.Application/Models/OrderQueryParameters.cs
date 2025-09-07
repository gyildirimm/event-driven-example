using OrderService.Domain.Enums;

namespace OrderService.Application.Models;

public class OrderQueryParameters
{
    public string? CustomerId { get; set; }
    public OrderStatus? Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
