using FluentValidation;

namespace PropertyOS.Application.Financials.Commands.RemindRentPayment;

public class RemindRentPaymentCommandValidator : AbstractValidator<RemindRentPaymentCommand>
{
    public RemindRentPaymentCommandValidator()
    {
        RuleFor(x => x.RentPaymentId)
            .NotEmpty()
            .WithMessage("RentPaymentId is required.");
    }
}
