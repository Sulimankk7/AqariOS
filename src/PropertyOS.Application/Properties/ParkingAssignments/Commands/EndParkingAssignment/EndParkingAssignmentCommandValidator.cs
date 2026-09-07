using FluentValidation;

namespace PropertyOS.Application.Properties.ParkingAssignments.Commands.EndParkingAssignment;

public class EndParkingAssignmentCommandValidator : AbstractValidator<EndParkingAssignmentCommand>
{
    public EndParkingAssignmentCommandValidator() => RuleFor(x => x.AssignmentId).NotEmpty();
}
