using FluentValidation;

namespace PropertyOS.Application.Financials.Commands.IssueRentPaymentReceipt;

public class IssueRentPaymentReceiptCommandValidator : AbstractValidator<IssueRentPaymentReceiptCommand>
{
    public IssueRentPaymentReceiptCommandValidator()
    {
        RuleFor(v => v.RentPaymentId)
            .NotEmpty().WithMessage("RentPaymentId is required.");
    }
}
