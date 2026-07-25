using System;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Properties.Enums;

namespace PropertyOS.Application.Properties.ParkingSpots.Commands.UpdateParkingSpot;

public record UpdateParkingSpotCommand(
    Guid Id,
    string SpotCode,
    ParkingType ParkingType,
    Guid? DefaultApartmentId = null,
    string? LocationDescription = null
) : ICommand;
