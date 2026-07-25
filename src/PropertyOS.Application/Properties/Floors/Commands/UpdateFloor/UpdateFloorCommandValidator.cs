using FluentValidation;

namespace PropertyOS.Application.Properties.Floors.Commands.UpdateFloor;

public class UpdateFloorCommandValidator : AbstractValidator<UpdateFloorCommand>
{
    public UpdateFloorCommandValidator()
    {
        RuleFor(v => v.Id)
            .NotEmpty().WithMessage("Floor ID is required.");

        RuleFor(v => v.FloorLabel)
            .NotEmpty().WithMessage("Floor label is required.")
            .MaximumLength(50).WithMessage("Floor label must not exceed 50 characters.");

        RuleFor(v => v.FloorType)
            .IsInEnum().WithMessage("Invalid floor type.");
    }
}
