using FluentValidation;
using NotificationService.Application.Models;

namespace NotificationService.Application.Validators;

public class NotificationEmailCreateModelValidator : AbstractValidator<NotificationEmailCreateModel>
{
    public NotificationEmailCreateModelValidator()
    {
        RuleFor(x => x.Recipient)
            .NotEmpty()
            .WithMessage("Alıcı email adresi boş olamaz")
            .EmailAddress()
            .WithMessage("Geçerli bir email adresi giriniz")
            .MaximumLength(254)
            .WithMessage("Email adresi en fazla 254 karakter olabilir");

        RuleFor(x => x.Subject)
            .NotEmpty()
            .WithMessage("Email konusu boş olamaz")
            .MinimumLength(3)
            .WithMessage("Email konusu en az 3 karakter olmalıdır")
            .MaximumLength(255)
            .WithMessage("Email konusu en fazla 255 karakter olabilir");

        RuleFor(x => x.Body)
            .NotEmpty()
            .WithMessage("Email içeriği boş olamaz")
            .MinimumLength(10)
            .WithMessage("Email içeriği en az 10 karakter olmalıdır")
            .MaximumLength(10000)
            .WithMessage("Email içeriği en fazla 10.000 karakter olabilir");
    }
}
