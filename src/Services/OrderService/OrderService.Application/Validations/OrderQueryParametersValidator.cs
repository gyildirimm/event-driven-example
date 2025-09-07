using FluentValidation;
using OrderService.Application.Models;

namespace OrderService.Application.Validations;

public class OrderQueryParametersValidator : AbstractValidator<OrderQueryParameters>
{
    public OrderQueryParametersValidator()
    {
        RuleFor(x => x.CustomerId)
            .MaximumLength(100)
            .WithMessage("Müşteri ID'si en fazla 100 karakter olabilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.CustomerId));

        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Sayfa numarası 0'dan büyük olmalıdır.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Sayfa boyutu 1 ile 100 arasında olmalıdır.");

        RuleFor(x => x.StartDate)
            .LessThanOrEqualTo(x => x.EndDate)
            .WithMessage("Başlangıç tarihi bitiş tarihinden küçük veya eşit olmalıdır.")
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue);

        RuleFor(x => x.EndDate)
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
            .WithMessage("Bitiş tarihi gelecek tarih olamaz.")
            .When(x => x.EndDate.HasValue);

        RuleFor(x => x.StartDate)
            .GreaterThanOrEqualTo(DateTime.UtcNow.AddYears(-5))
            .WithMessage("Başlangıç tarihi 5 yıldan eski olamaz.")
            .When(x => x.StartDate.HasValue);
    }
}
