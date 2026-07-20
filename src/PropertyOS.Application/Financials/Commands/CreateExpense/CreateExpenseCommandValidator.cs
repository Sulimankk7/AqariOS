using System;
using System.Linq;
using FluentValidation;

namespace PropertyOS.Application.Financials.Commands.CreateExpense;

public class CreateExpenseCommandValidator : AbstractValidator<CreateExpenseCommand>
{
    public CreateExpenseCommandValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Expense amount must be positive.");

        RuleFor(x => x.ExpenseDate)
            .LessThanOrEqualTo(x => DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Expense date cannot be in the future.");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Expense description must be specified.");

        RuleFor(x => x.Receipts)
            .Must(receipts => receipts == null || receipts.All(r => r.Amount > 0))
            .WithMessage("All receipt amounts must be positive.")
            .Must(receipts => receipts == null || receipts.All(r => r.IssuedAt <= DateOnly.FromDateTime(DateTime.UtcNow)))
            .WithMessage("All receipt issued dates must be in the past or present.");
    }
}
