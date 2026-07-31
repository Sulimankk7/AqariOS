using FluentValidation;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials.Commands.RecordManualRentPayment;

public class RecordManualRentPaymentCommandValidator : AbstractValidator<RecordManualRentPaymentCommand>
{
    public RecordManualRentPaymentCommandValidator()
    {
        RuleFor(v => v.LeaseContractId)
            .NotEmpty().WithMessage("LeaseContractId is required.");

        RuleFor(v => v.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(v => v.PaymentMethod)
            .IsInEnum().WithMessage("Method must be a valid payment method.");

        RuleFor(v => v.PaymentReferenceNumber)
            .MaximumLength(255).WithMessage("PaymentReferenceNumber must not exceed 255 characters.");

        RuleFor(v => v.Cheque)
            .NotNull()
            .When(v => v.PaymentMethod == PaymentMethod.Cheque)
            .WithMessage("Cheque details are required when the payment method is Cheque.");

        When(v => v.Cheque != null, () =>
        {
            RuleFor(v => v.Cheque!.ChequeNumber)
                .NotEmpty().WithMessage("ChequeNumber is required.")
                .MaximumLength(100).WithMessage("ChequeNumber must not exceed 100 characters.");

            RuleFor(v => v.Cheque!.BankName)
                .NotEmpty().WithMessage("BankName is required.")
                .MaximumLength(255).WithMessage("BankName must not exceed 255 characters.");

            RuleFor(v => v.Cheque!)
                .Must(c => c.DueDate >= c.IssueDate)
                .WithMessage("Cheque due date cannot be before its issue date.")
                .Must(c => !c.ReceivedDate.HasValue || c.ReceivedDate.Value >= c.IssueDate)
                .WithMessage("Cheque received date cannot be before its issue date.");
        });

        RuleForEach(v => v.Allocations).ChildRules(alloc =>
        {
            alloc.RuleFor(a => a.ObligationPaymentId)
                .NotEmpty().WithMessage("ObligationPaymentId is required.");

            alloc.RuleFor(a => a.Amount)
                .GreaterThan(0).WithMessage("Allocation amount must be greater than zero.");
        });
    }
}
