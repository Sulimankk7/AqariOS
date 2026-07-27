using FluentValidation;

namespace PropertyOS.Application.Financials.Commands.MarkRentPaymentOverdue;

public class MarkRentPaymentOverdueCommandValidator : AbstractValidator<MarkRentPaymentOverdueCommand>
{
    public MarkRentPaymentOverdueCommandValidator()
    {
        RuleFor(v => v.RentPaymentId)
            .NotEmpty().WithMessage("RentPaymentId is required.");
    }
}
