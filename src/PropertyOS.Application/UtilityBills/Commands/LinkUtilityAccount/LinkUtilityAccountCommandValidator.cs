using FluentValidation;
using PropertyOS.Domain.UtilityBills.Enums;

namespace PropertyOS.Application.UtilityBills.Commands.LinkUtilityAccount;

public sealed class LinkUtilityAccountCommandValidator : AbstractValidator<LinkUtilityAccountCommand>
{
    public LinkUtilityAccountCommandValidator()
    {
        RuleFor(x => x.LeaseContractId)
            .NotEmpty();

        RuleFor(x => x.UtilityType)
            .IsInEnum();

        RuleFor(x => x.AccountNumber)
            .NotEmpty()
            .Must((command, accountNumber) =>
                UtilityAccountNumberRules.IsValid(command.UtilityType, accountNumber))
            .WithMessage(command => command.UtilityType == UtilityType.Electricity
                ? "The electricity account number must contain exactly 10 digits."
                : "The water subscription number must contain between 1 and 20 digits.");

        RuleFor(x => x.MeterNumber)
            .MaximumLength(100)
            .When(x => x.MeterNumber is not null)
            .WithMessage("Meter number must not exceed 100 characters.");
    }
}
