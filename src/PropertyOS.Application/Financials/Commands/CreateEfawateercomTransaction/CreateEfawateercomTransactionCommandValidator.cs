using System;
using FluentValidation;

namespace PropertyOS.Application.Financials.Commands.CreateEfawateercomTransaction;

public class CreateEfawateercomTransactionCommandValidator : AbstractValidator<CreateEfawateercomTransactionCommand>
{
    public CreateEfawateercomTransactionCommandValidator()
    {
        RuleFor(x => x.RentPaymentId)
            .NotEmpty()
            .WithMessage("Rent payment ID must be specified.");

        RuleFor(x => x.ExternalTransactionId)
            .NotEmpty()
            .WithMessage("External transaction ID must be specified.");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Transaction amount must be positive.");
    }
}
