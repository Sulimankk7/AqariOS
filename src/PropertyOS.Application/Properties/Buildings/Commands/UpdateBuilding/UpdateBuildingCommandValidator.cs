using FluentValidation;
using PropertyOS.Domain.Properties;

namespace PropertyOS.Application.Properties.Buildings.Commands.UpdateBuilding;

public class UpdateBuildingCommandValidator : AbstractValidator<UpdateBuildingCommand>
{
    public UpdateBuildingCommandValidator()
    {
        RuleFor(v => v.Id)
            .NotEmpty().WithMessage("Building ID is required.");

        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Building name is required.")
            .MaximumLength(100).WithMessage("Building name must not exceed 100 characters.");

        RuleFor(v => v.InternalCode)
            .MaximumLength(50).WithMessage("Internal code must not exceed 50 characters.");

        RuleFor(v => v.AddressGovernorate)
            .IsInEnum().WithMessage("Invalid governorate.");

        RuleFor(v => v.AddressCity)
            .NotEmpty().WithMessage("City is required.")
            .MaximumLength(100).WithMessage("City must not exceed 100 characters.");

        RuleFor(v => v.AddressNeighborhood)
            .NotEmpty().WithMessage("Neighborhood is required.")
            .MaximumLength(100).WithMessage("Neighborhood must not exceed 100 characters.");

        RuleFor(v => v.AddressStreet)
            .MaximumLength(100).WithMessage("Street must not exceed 100 characters.");

        RuleFor(v => v.AddressPostalCode)
            .MaximumLength(20).WithMessage("Postal code must not exceed 20 characters.");

        // Both GPS coordinates or neither
        RuleFor(v => v)
            .Must(v => (v.GpsLatitude.HasValue && v.GpsLongitude.HasValue) || (!v.GpsLatitude.HasValue && !v.GpsLongitude.HasValue))
            .WithMessage("GPS Latitude and Longitude must both be provided or both be null.")
            .WithName("GpsCoordinates");
    }
}
