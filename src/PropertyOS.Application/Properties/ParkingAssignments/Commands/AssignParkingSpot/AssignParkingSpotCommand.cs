using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Properties.ParkingAssignments.Commands.AssignParkingSpot;

public record AssignParkingSpotCommand(
    Guid ParkingSpotId,
    Guid LeaseContractId
) : ICommand<Guid>;
