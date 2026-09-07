using FluentValidation;

namespace PropertyOS.Application.Properties.ParkingAssignments.Commands.AssignParkingSpot;

public class AssignParkingSpotCommandValidator : AbstractValidator<AssignParkingSpotCommand>
{
    public AssignParkingSpotCommandValidator()
    {
        RuleFor(v => v.ParkingSpotId).NotEmpty();
        RuleFor(v => v.LeaseContractId).NotEmpty();
    }
}
