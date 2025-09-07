using FluentValidation;
using StockService.Application.DTOs;

namespace StockService.Application.Validations;

public class StockQueryParametersValidator : AbstractValidator<StockQueryParameters>
{
    public StockQueryParametersValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Sayfa numarası en az 1 olmalıdır.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Sayfa boyutu en az 1 olmalıdır.")
            .LessThanOrEqualTo(100)
            .WithMessage("Sayfa boyutu en fazla 100 olabilir.");

        RuleFor(x => x.MinQuantity)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Minimum miktar negatif olamaz.")
            .When(x => x.MinQuantity.HasValue);

        RuleFor(x => x.MaxQuantity)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Maksimum miktar negatif olamaz.")
            .When(x => x.MaxQuantity.HasValue);

        RuleFor(x => x)
            .Must(x => !x.MinQuantity.HasValue || !x.MaxQuantity.HasValue || x.MinQuantity.Value <= x.MaxQuantity.Value)
            .WithMessage("Minimum miktar, maksimum miktardan büyük olamaz.")
            .When(x => x.MinQuantity.HasValue && x.MaxQuantity.HasValue);
    }
}
