using FluentValidation;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials.Commands.SubmitPaymentRequest;

public class SubmitPaymentRequestCommandValidator : AbstractValidator<SubmitPaymentRequestCommand>
{
    public SubmitPaymentRequestCommandValidator()
    {
        RuleFor(v => v.RentPaymentId)
            .NotEmpty().WithMessage("Rent payment ID is required.");

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
            RuleFor(v => v.ReferenceNumber)
                .NotEmpty().WithMessage("Cheque number is required.");
        });
    }
}
