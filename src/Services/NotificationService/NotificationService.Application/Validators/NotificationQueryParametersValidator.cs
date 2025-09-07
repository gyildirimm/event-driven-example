using FluentValidation;
using NotificationService.Application.Models;

namespace NotificationService.Application.Validators;

public class NotificationQueryParametersValidator : AbstractValidator<NotificationQueryParameters>
{
    public NotificationQueryParametersValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Sayfa numarası 1'den büyük olmalıdır");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .WithMessage("Sayfa boyutu 1'den büyük olmalıdır")
            .LessThanOrEqualTo(100)
            .WithMessage("Sayfa boyutu en fazla 100 olabilir");

        RuleFor(x => x.Recipient)
            .MaximumLength(254)
            .WithMessage("Alıcı bilgisi en fazla 254 karakter olabilir")
            .When(x => !string.IsNullOrEmpty(x.Recipient));

        RuleFor(x => x.StartDate)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Başlangıç tarihi gelecek bir tarih olamaz")
            .When(x => x.StartDate.HasValue);

        RuleFor(x => x.EndDate)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Bitiş tarihi gelecek bir tarih olamaz")
            .When(x => x.EndDate.HasValue);

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("Bitiş tarihi başlangıç tarihinden önce olamaz")
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue);
    }
}
