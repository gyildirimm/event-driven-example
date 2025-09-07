namespace StockService.Application.DTOs;

public class StockReservationDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid OrderId { get; set; }
    public int ReservedQuantity { get; set; }
    public DateTime ReservationDate { get; set; }
    public bool IsConfirmed { get; set; }
}
