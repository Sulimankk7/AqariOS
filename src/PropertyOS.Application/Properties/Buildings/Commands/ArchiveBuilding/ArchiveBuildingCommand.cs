using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Properties.Buildings.Commands.ArchiveBuilding;

public record ArchiveBuildingCommand(Guid Id) : ICommand;
