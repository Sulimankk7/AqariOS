using FluentValidation;

namespace PropertyOS.Application.Financials.Commands.GenerateScheduledInstallments;

public class GenerateScheduledInstallmentsCommandValidator : AbstractValidator<GenerateScheduledInstallmentsCommand>
{
    public GenerateScheduledInstallmentsCommandValidator()
    {
        RuleFor(v => v.LeaseContractId)
            .NotEmpty().WithMessage("LeaseContractId is required.");
    }
}
