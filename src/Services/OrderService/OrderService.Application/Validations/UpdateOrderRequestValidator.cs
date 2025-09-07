using FluentValidation;
using OrderService.Application.Models;
using OrderService.Domain.Enums;

namespace OrderService.Application.Validations;

public class UpdateOrderRequestValidator : AbstractValidator<UpdateOrderRequest>
{
    public UpdateOrderRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Geçerli bir sipariş durumu seçiniz.");
    }
}
