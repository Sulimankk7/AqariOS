using FluentValidation;

namespace PropertyOS.Application.Maintenance.Commands.DeleteMaintenanceRequest;

public class DeleteMaintenanceRequestCommandValidator : AbstractValidator<DeleteMaintenanceRequestCommand>
{
    public DeleteMaintenanceRequestCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Request ID must be specified.");
    }
}
