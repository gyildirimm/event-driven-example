using FluentValidation;
using NotificationService.Application.Models;

namespace NotificationService.Application.Validators;

public class NotificationSmsCreateModelValidator : AbstractValidator<NotificationSmsCreateModel>
{
    public NotificationSmsCreateModelValidator()
    {
        RuleFor(x => x.Recipient)
            .NotEmpty()
            .WithMessage("Alıcı telefon numarası boş olamaz")
            .MinimumLength(10)
            .WithMessage("Telefon numarası en az 10 karakter olmalıdır")
            .MaximumLength(15)
            .WithMessage("Telefon numarası en fazla 15 karakter olabilir")
            .Matches(@"^\+?[1-9]\d{1,14}$")
            .WithMessage("Geçerli bir telefon numarası formatı giriniz (örn: +905551234567)");

        RuleFor(x => x.Text)
            .NotEmpty()
            .WithMessage("SMS metni boş olamaz")
            .MinimumLength(1)
            .WithMessage("SMS metni en az 1 karakter olmalıdır")
            .MaximumLength(160)
            .WithMessage("SMS metni en fazla 160 karakter olabilir");
    }
}
