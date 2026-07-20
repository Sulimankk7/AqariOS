using System;
using FluentValidation;

namespace PropertyOS.Application.Maintenance.Commands.CreateMaintenanceRequest;

public class CreateMaintenanceRequestCommandValidator : AbstractValidator<CreateMaintenanceRequestCommand>
{
    public CreateMaintenanceRequestCommandValidator()
    {
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

        RuleFor(x => x.RequestDate)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Request date cannot be in the future.");
    }
}
