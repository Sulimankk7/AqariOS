using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Properties.Apartments.Commands.ArchiveApartment;

public record ArchiveApartmentCommand(Guid Id) : ICommand;
