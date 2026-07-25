using FluentValidation;

namespace PropertyOS.Application.Properties.ParkingSpots.Commands.CreateParkingSpot;

public class CreateParkingSpotCommandValidator : AbstractValidator<CreateParkingSpotCommand>
{
    public CreateParkingSpotCommandValidator()
    {
        RuleFor(v => v.BuildingId)
            .NotEmpty().WithMessage("Building ID is required.");

        RuleFor(v => v.SpotCode)
            .NotEmpty().WithMessage("Spot code is required.")
            .MaximumLength(50).WithMessage("Spot code must not exceed 50 characters.");

        RuleFor(v => v.ParkingType)
            .IsInEnum().WithMessage("Invalid parking type.");

        RuleFor(v => v.LocationDescription)
            .MaximumLength(255).WithMessage("Location description must not exceed 255 characters.");
    }
}
