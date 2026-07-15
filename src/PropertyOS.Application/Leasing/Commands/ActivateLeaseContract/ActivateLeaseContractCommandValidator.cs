using FluentValidation;

namespace PropertyOS.Application.Leasing.Commands.ActivateLeaseContract;

public class ActivateLeaseContractCommandValidator : AbstractValidator<ActivateLeaseContractCommand>
{
    public ActivateLeaseContractCommandValidator()
    {
        RuleFor(v => v.ContractId)
            .NotEmpty().WithMessage("ContractId is required.");
    }
}
