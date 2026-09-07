using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Properties.ParkingAssignments.Commands.EndParkingAssignment;

public record EndParkingAssignmentCommand(Guid AssignmentId) : ICommand;
