using System;
using FluentValidation;

namespace PropertyOS.Application.Financials.Commands.UpdateExpense;

public class UpdateExpenseCommandValidator : AbstractValidator<UpdateExpenseCommand>
{
    public UpdateExpenseCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Expense ID must be specified.");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Expense amount must be positive.");

        RuleFor(x => x.ExpenseDate)
            .LessThanOrEqualTo(x => DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Expense date cannot be in the future.");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Expense description must be specified.");
    }
}
