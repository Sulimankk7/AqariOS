using System;
using FluentValidation;

namespace PropertyOS.Application.Leasing.Commands.TerminateLeaseContract;

public class TerminateLeaseContractCommandValidator : AbstractValidator<TerminateLeaseContractCommand>
{
    public TerminateLeaseContractCommandValidator()
    {
        RuleFor(v => v.ContractId)
            .NotEmpty().WithMessage("ContractId is required.");

        RuleFor(v => v.TerminationDate)
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Termination date must not be in the future.");

        RuleFor(v => v.OutstandingBalance)
            .GreaterThanOrEqualTo(0).WithMessage("OutstandingBalance cannot be negative.");

        RuleFor(v => v.DepositReturnedAmount)
            .GreaterThanOrEqualTo(0).WithMessage("DepositReturnedAmount cannot be negative.");

        RuleFor(v => v.DepositDeductionAmount)
            .GreaterThanOrEqualTo(0).WithMessage("DepositDeductionAmount cannot be negative.");

        RuleFor(v => v.DepositDeductionReason)
            .NotEmpty().When(v => v.DepositDeductionAmount > 0)
            .WithMessage("Deposit deduction reason is required when deduction amount is greater than zero.");
            
        RuleFor(v => v.TerminationType)
            .IsInEnum().WithMessage("TerminationType must be a valid enum value.");
    }
}
