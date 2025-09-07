using FluentValidation;
using StockService.Application.DTOs;

namespace StockService.Application.Validations;

public class ReserveStockRequestValidator : AbstractValidator<ReserveStockRequest>
{
    public ReserveStockRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Ürün ID'si boş olamaz.");

        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("Sipariş ID'si boş olamaz.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Rezerve edilecek miktar sıfırdan büyük olmalıdır.")
            .LessThanOrEqualTo(10000)
            .WithMessage("Bir defada en fazla 10.000 adet rezerve edilebilir.");
    }
}
