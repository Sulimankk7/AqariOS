using FluentValidation;

namespace PropertyOS.Application.UtilityBills.Commands.TenantReplaceUtilityAccount;

public sealed class TenantReplaceUtilityAccountCommandValidator
    : AbstractValidator<TenantReplaceUtilityAccountCommand>
{
    public TenantReplaceUtilityAccountCommandValidator()
    {
        RuleFor(command => command.UtilityAccountId).NotEmpty();
        RuleFor(command => command.AccountNumber)
            .NotEmpty()
            .MaximumLength(32)
            .Matches(@"^\d+$")
            .WithMessage("The utility account number must contain digits only.");
        RuleFor(command => command.MeterNumber)
            .MaximumLength(100)
            .When(command => command.MeterNumber is not null);
    }
}
