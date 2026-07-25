using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Properties.Floors.Commands.ArchiveFloor;

public record ArchiveFloorCommand(Guid Id) : ICommand;
