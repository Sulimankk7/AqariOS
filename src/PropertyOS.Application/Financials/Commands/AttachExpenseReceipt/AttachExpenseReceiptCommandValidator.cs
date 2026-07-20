using System;
using FluentValidation;

namespace PropertyOS.Application.Financials.Commands.AttachExpenseReceipt;

public class AttachExpenseReceiptCommandValidator : AbstractValidator<AttachExpenseReceiptCommand>
{
    public AttachExpenseReceiptCommandValidator()
    {
        RuleFor(x => x.ExpenseId)
            .NotEmpty()
            .WithMessage("Expense ID must be specified.");

        RuleFor(x => x.FileId)
            .NotEmpty()
            .WithMessage("File ID must be specified.");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Receipt amount must be positive.");

        RuleFor(x => x.IssuedAt)
            .LessThanOrEqualTo(x => DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Receipt issue date cannot be in the future.");
    }
}
