using FluentValidation;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials.Commands.RecordChequeStatusChange;

public class RecordChequeStatusChangeCommandValidator : AbstractValidator<RecordChequeStatusChangeCommand>
{
    public RecordChequeStatusChangeCommandValidator()
    {
        RuleFor(v => v.ChequeId)
            .NotEmpty().WithMessage("ChequeId is required.");

        RuleFor(v => v.NewStatus)
            .IsInEnum().WithMessage("A valid target ChequeStatus is required.");

        RuleFor(v => v.ActionDate)
            .NotEmpty().WithMessage("ActionDate is required.");

        RuleFor(v => v.BounceReason)
            .NotEmpty().When(v => v.NewStatus == ChequeStatus.Bounced)
            .WithMessage("BounceReason is required when status is Bounced.")
            .MaximumLength(255).WithMessage("BounceReason must not exceed 255 characters.");

        RuleFor(v => v.CancellationReason)
            .NotEmpty().When(v => v.NewStatus == ChequeStatus.Cancelled)
            .WithMessage("CancellationReason is required when status is Cancelled.")
            .MaximumLength(255).WithMessage("CancellationReason must not exceed 255 characters.");
    }
}
