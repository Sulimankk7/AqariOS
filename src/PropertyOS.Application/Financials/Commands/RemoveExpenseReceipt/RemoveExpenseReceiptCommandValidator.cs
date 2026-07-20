using System;
using FluentValidation;

namespace PropertyOS.Application.Financials.Commands.RemoveExpenseReceipt;

public class RemoveExpenseReceiptCommandValidator : AbstractValidator<RemoveExpenseReceiptCommand>
{
    public RemoveExpenseReceiptCommandValidator()
    {
        RuleFor(x => x.ExpenseId)
            .NotEmpty()
            .WithMessage("Expense ID must be specified.");

        RuleFor(x => x.ReceiptId)
            .NotEmpty()
            .WithMessage("Receipt ID must be specified.");
    }
}
