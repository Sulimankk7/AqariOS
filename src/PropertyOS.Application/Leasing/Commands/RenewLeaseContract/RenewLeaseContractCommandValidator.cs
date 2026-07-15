using FluentValidation;

namespace PropertyOS.Application.Leasing.Commands.RenewLeaseContract;

public class RenewLeaseContractCommandValidator : AbstractValidator<RenewLeaseContractCommand>
{
    public RenewLeaseContractCommandValidator()
    {
        RuleFor(v => v.PriorContractId)
            .NotEmpty().WithMessage("PriorContractId is required.");

        RuleFor(v => v.ContractNumber)
            .NotEmpty().WithMessage("ContractNumber is required.")
            .Must(s => !string.IsNullOrWhiteSpace(s)).WithMessage("ContractNumber cannot be whitespace.");

        RuleFor(v => v.StartDate)
            .NotEmpty().WithMessage("StartDate is required.");

        RuleFor(v => v.EndDate)
            .GreaterThan(v => v.StartDate).WithMessage("EndDate must be strictly greater than StartDate.");

        RuleFor(v => v.MonthlyRentAmount)
            .GreaterThan(0).WithMessage("MonthlyRentAmount must be greater than zero.");

        RuleFor(v => v.SecurityDepositAmount)
            .GreaterThanOrEqualTo(0).WithMessage("SecurityDepositAmount cannot be negative.");

        RuleFor(v => v.PaymentDueDay)
            .InclusiveBetween((short)1, (short)28).WithMessage("PaymentDueDay must be between 1 and 28.");
    }
}
