using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Properties.Apartments.Commands.UpdateApartment;

public record UpdateApartmentCommand(
    Guid Id,
    decimal? BaseRentAmount,
    string BaseRentCurrency = "JOD"
) : ICommand;
