using System;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Properties.Enums;

namespace PropertyOS.Application.Properties.ParkingSpots.Commands.CreateParkingSpot;

public record CreateParkingSpotCommand(
    Guid BuildingId,
    string SpotCode,
    ParkingType ParkingType = ParkingType.Standard,
    Guid? DefaultApartmentId = null,
    string? LocationDescription = null
) : ICommand;
