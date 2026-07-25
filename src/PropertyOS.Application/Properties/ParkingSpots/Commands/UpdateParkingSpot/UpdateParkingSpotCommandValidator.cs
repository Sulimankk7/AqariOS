using FluentValidation;

namespace PropertyOS.Application.Properties.ParkingSpots.Commands.UpdateParkingSpot;

public class UpdateParkingSpotCommandValidator : AbstractValidator<UpdateParkingSpotCommand>
{
    public UpdateParkingSpotCommandValidator()
    {
        RuleFor(v => v.Id)
            .NotEmpty().WithMessage("Parking spot ID is required.");

        RuleFor(v => v.SpotCode)
            .NotEmpty().WithMessage("Spot code is required.")
            .MaximumLength(50).WithMessage("Spot code must not exceed 50 characters.");

        RuleFor(v => v.ParkingType)
            .IsInEnum().WithMessage("Invalid parking type.");

        RuleFor(v => v.LocationDescription)
            .MaximumLength(255).WithMessage("Location description must not exceed 255 characters.");
    }
}
