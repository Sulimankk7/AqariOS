using System;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Properties.Enums;

namespace PropertyOS.Application.Properties.Floors.Commands.UpdateFloor;

public record UpdateFloorCommand(
    Guid Id,
    string FloorLabel,
    FloorType FloorType
) : ICommand;
