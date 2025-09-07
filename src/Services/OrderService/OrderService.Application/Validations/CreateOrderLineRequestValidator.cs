using FluentValidation;
using OrderService.Application.Models;

namespace OrderService.Application.Validations;

public class CreateOrderLineRequestValidator : AbstractValidator<CreateOrderLineRequest>
{
    public CreateOrderLineRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Ürün ID'si boş olamaz.")
            .MaximumLength(100)
            .WithMessage("Ürün ID'si en fazla 100 karakter olabilir.");

        RuleFor(x => x.ProductName)
            .NotEmpty()
            .WithMessage("Ürün adı boş olamaz.")
            .MaximumLength(200)
            .WithMessage("Ürün adı en fazla 200 karakter olabilir.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Miktar 0'dan büyük olmalıdır.")
            .LessThanOrEqualTo(1000)
            .WithMessage("Miktar 1000'den fazla olamaz.");

        RuleFor(x => x.UnitPrice)
            .GreaterThan(0)
            .WithMessage("Birim fiyat 0'dan büyük olmalıdır.")
            .Must(price => decimal.Round(price, 2) == price)
            .WithMessage("Birim fiyat en fazla 2 ondalık basamak içerebilir.")
            .LessThan(1000000)
            .WithMessage("Birim fiyat 1.000.000'dan küçük olmalıdır.");
    }
}
