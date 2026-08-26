using FluentValidation;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials.Commands.SubmitPaymentRequest;

public class SubmitPaymentRequestCommandValidator : AbstractValidator<SubmitPaymentRequestCommand>
{
    public SubmitPaymentRequestCommandValidator()
    {
        RuleFor(v => v.RentPaymentId)
            .NotEmpty().WithMessage("Rent payment ID is required.");

        RuleFor(v => v.Amount)
            .GreaterThan(0).WithMessage("Submitted payment amount must be greater than zero.")
            .Must(a => decimal.Round(a, 3) == a).WithMessage("Payment amount must not exceed 3 decimal places.");

        RuleFor(v => v.PaymentMethod)
            .IsInEnum().WithMessage("Invalid payment method.");

        When(v => v.PaymentMethod == PaymentMethod.CliQ || v.PaymentMethod == PaymentMethod.BankTransfer, () =>
        {
            RuleFor(v => v.ProofFileId)
                .NotEmpty().WithMessage("Proof of payment is required for CliQ and Bank Transfer.");
            
            RuleFor(v => v.ReferenceNumber)
                .NotEmpty().WithMessage("Reference number is required for CliQ and Bank Transfer.");
        });

        When(v => v.PaymentMethod == PaymentMethod.Cheque, () =>
        {
            RuleFor(v => v.ChequeDetails)
                .NotNull().WithMessage("Cheque details (cheque number, bank name, issue date, and due date) are required when the payment method is Cheque.");

            When(v => v.ChequeDetails != null, () =>
            {
                RuleFor(v => v.ChequeDetails!.ChequeNumber)
                    .NotEmpty().WithMessage("Cheque number is required.")
                    .MaximumLength(100).WithMessage("Cheque number must not exceed 100 characters.");

                RuleFor(v => v.ChequeDetails!.BankName)
                    .NotEmpty().WithMessage("Bank name is required.")
                    .MaximumLength(255).WithMessage("Bank name must not exceed 255 characters.");

                RuleFor(v => v.ChequeDetails!)
                    .Must(c => c.DueDate >= c.IssueDate)
                    .WithMessage("Cheque due date cannot be before its issue date.");
            });
        });
    }
}
