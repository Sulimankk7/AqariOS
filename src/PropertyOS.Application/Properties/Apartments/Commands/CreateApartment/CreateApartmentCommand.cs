using System;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Properties.Enums;

namespace PropertyOS.Application.Properties.Apartments.Commands.CreateApartment;

public record CreateApartmentCommand(
    Guid FloorId,
    string UnitNumber,
    decimal AreaSqm,
    OwnershipStatus OwnershipStatus = OwnershipStatus.CompanyOwned,
    string? ExternalOwnerName = null,
    string? ExternalOwnerPhone = null,
    short Bedrooms = 0,
    short Bathrooms = 0,
    decimal? BaseRentAmount = null,
    string BaseRentCurrency = "JOD"
) : ICommand;
