using FluentValidation;

namespace PropertyOS.Application.Financials.Commands.ReversePaymentAllocation;

public class ReversePaymentAllocationCommandValidator : AbstractValidator<ReversePaymentAllocationCommand>
{
    public ReversePaymentAllocationCommandValidator()
    {
        RuleFor(v => v.AllocationId)
            .NotEmpty().WithMessage("AllocationId is required.");

        RuleFor(v => v.ReversalReason)
            .NotEmpty().WithMessage("ReversalReason is required.")
            .MaximumLength(255).WithMessage("ReversalReason must not exceed 255 characters.");
    }
}
