using FluentValidation;

namespace PropertyOS.Application.Financials.Commands.RecordPaymentAllocation;

public class RecordPaymentAllocationCommandValidator : AbstractValidator<RecordPaymentAllocationCommand>
{
    public RecordPaymentAllocationCommandValidator()
    {
        RuleFor(v => v.ReceivingPaymentId)
            .NotEmpty().WithMessage("ReceivingPaymentId is required.");

        RuleFor(v => v.AllocationDate)
            .NotEmpty().WithMessage("AllocationDate is required.");

        RuleFor(v => v.Allocations)
            .NotEmpty().WithMessage("At least one allocation detail is required.");

        RuleForEach(v => v.Allocations).ChildRules(alloc =>
        {
            alloc.RuleFor(a => a.ObligationPaymentId)
                .NotEmpty().WithMessage("ObligationPaymentId is required.");

            alloc.RuleFor(a => a.Amount)
                .GreaterThan(0).WithMessage("Allocation amount must be greater than zero.");
        });
    }
}
