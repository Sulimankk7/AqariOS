using FluentValidation;

namespace PropertyOS.Application.Maintenance.Commands.UpdateMaintenanceRequestStatus;

public class UpdateMaintenanceRequestStatusCommandValidator : AbstractValidator<UpdateMaintenanceRequestStatusCommand>
{
    public UpdateMaintenanceRequestStatusCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Request ID must be specified.");

        RuleFor(x => x.NewStatus)
            .IsInEnum()
            .WithMessage("New status must be a valid maintenance status.");

        RuleFor(x => x.Reason)
            // Column is unbounded text; 1000 is an application-level sanity bound.
            .MaximumLength(1000)
            .WithMessage("Reason must not exceed 1000 characters.");
    }
}
