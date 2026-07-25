using FluentValidation;

namespace PropertyOS.Application.Properties.Floors.Commands.CreateFloor;

public class CreateFloorCommandValidator : AbstractValidator<CreateFloorCommand>
{
    public CreateFloorCommandValidator()
    {
        RuleFor(v => v.BuildingId)
            .NotEmpty().WithMessage("Building ID is required.");

        RuleFor(v => v.FloorLabel)
            .NotEmpty().WithMessage("Floor label is required.")
            .MaximumLength(50).WithMessage("Floor label must not exceed 50 characters.");

        RuleFor(v => v.FloorType)
            .IsInEnum().WithMessage("Invalid floor type.");

        RuleFor(v => v.FloorNumber)
            .InclusiveBetween((short)-50, (short)200)
            .WithMessage("Floor number must be between -50 and 200.");
    }
}
