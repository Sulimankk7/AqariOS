using FluentValidation;

namespace PropertyOS.Application.Financials.Commands.CancelRentPayment;

public class CancelRentPaymentCommandValidator : AbstractValidator<CancelRentPaymentCommand>
{
    public CancelRentPaymentCommandValidator()
    {
        RuleFor(v => v.RentPaymentId)
            .NotEmpty().WithMessage("RentPaymentId is required.");

        RuleFor(v => v.Reason)
            .MaximumLength(255).WithMessage("Reason must not exceed 255 characters.");
    }
}
