namespace StockService.Application.DTOs;

public class StockQueryParameters
{
    public Guid? ProductId { get; set; }
    public int? MinQuantity { get; set; }
    public int? MaxQuantity { get; set; }
    public bool? OnlyAvailable { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
