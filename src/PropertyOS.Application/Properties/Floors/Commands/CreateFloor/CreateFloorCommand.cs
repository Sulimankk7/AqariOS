using System;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Properties.Enums;

namespace PropertyOS.Application.Properties.Floors.Commands.CreateFloor;

public record CreateFloorCommand(
    Guid BuildingId,
    short FloorNumber,
    string FloorLabel,
    FloorType FloorType
) : ICommand;
