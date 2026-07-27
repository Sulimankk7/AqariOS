using FluentValidation;

namespace PropertyOS.Application.Leasing.Commands.ExpireLeaseContract;

public class ExpireLeaseContractCommandValidator : AbstractValidator<ExpireLeaseContractCommand>
{
    public ExpireLeaseContractCommandValidator()
    {
        RuleFor(v => v.ContractId)
            .NotEmpty().WithMessage("ContractId is required.");
    }
}
