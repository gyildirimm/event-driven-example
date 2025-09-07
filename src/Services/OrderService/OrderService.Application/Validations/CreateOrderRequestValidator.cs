using FluentValidation;
using OrderService.Application.Models;

namespace OrderService.Application.Validations;

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("Müşteri ID'si boş olamaz.")
            .MaximumLength(100)
            .WithMessage("Müşteri ID'si en fazla 100 karakter olabilir.");

        RuleFor(x => x.CustomerEmail)
            .NotEmpty()
            .WithMessage("Müşteri e-posta adresi boş olamaz.")
            .EmailAddress()
            .WithMessage("Geçerli bir e-posta adresi giriniz.")
            .MaximumLength(255)
            .WithMessage("E-posta adresi en fazla 255 karakter olabilir.");

        RuleFor(x => x.OrderLines)
            .NotEmpty()
            .WithMessage("Sipariş kalemler listesi boş olamaz.")
            .Must(orderLines => orderLines.Count <= 50)
            .WithMessage("Bir siparişte en fazla 50 kalem olabilir.");

        RuleForEach(x => x.OrderLines)
            .SetValidator(new CreateOrderLineRequestValidator());

        // Business rule: OrderLines'da aynı ProductId'den birden fazla olmamalı
        RuleFor(x => x.OrderLines)
            .Must(orderLines => orderLines.GroupBy(ol => ol.ProductId).All(g => g.Count() == 1))
            .WithMessage("Aynı üründen birden fazla sipariş kalemi olamaz. Miktarları birleştirin.")
            .When(x => x.OrderLines != null && x.OrderLines.Any());
    }
}
