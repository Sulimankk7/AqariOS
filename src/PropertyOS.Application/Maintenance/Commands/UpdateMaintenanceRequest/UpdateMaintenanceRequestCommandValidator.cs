using System;
using FluentValidation;

namespace PropertyOS.Application.Maintenance.Commands.UpdateMaintenanceRequest;

public class UpdateMaintenanceRequestCommandValidator : AbstractValidator<UpdateMaintenanceRequestCommand>
{
    public UpdateMaintenanceRequestCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Request ID must be specified.");

        RuleFor(x => x.BuildingId)
            .NotEmpty()
            .WithMessage("Building ID must be specified.");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title must not be blank.")
            .MaximumLength(255)
            .WithMessage("Title must not exceed 255 characters.");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description must not be blank.");
    }
}
