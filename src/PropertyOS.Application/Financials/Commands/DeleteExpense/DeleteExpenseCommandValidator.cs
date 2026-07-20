using System;
using FluentValidation;

namespace PropertyOS.Application.Financials.Commands.DeleteExpense;

public class DeleteExpenseCommandValidator : AbstractValidator<DeleteExpenseCommand>
{
    public DeleteExpenseCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Expense ID must be specified.");
    }
}
