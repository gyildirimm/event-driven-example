using FluentValidation;
using StockService.Application.DTOs;

namespace StockService.Application.Validations;

public class UpdateStockRequestValidator : AbstractValidator<UpdateStockRequest>
{
    public UpdateStockRequestValidator()
    {
        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Stok miktarı negatif olamaz.")
            .LessThanOrEqualTo(1000000)
            .WithMessage("Stok miktarı en fazla 1.000.000 olabilir.");
    }
}
