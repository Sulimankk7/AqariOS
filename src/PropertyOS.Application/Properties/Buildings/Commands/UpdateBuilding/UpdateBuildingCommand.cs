using System;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Properties;
using PropertyOS.Domain.Properties.Enums;
namespace PropertyOS.Application.Properties.Buildings.Commands.UpdateBuilding;

public record UpdateBuildingCommand(
    Guid Id,
    string Name,
    BuildingType BuildingType,
    string? InternalCode,
    short? ConstructionYear,
    decimal? GpsLatitude,
    decimal? GpsLongitude,
    Governorate AddressGovernorate,
    string AddressCity,
    string AddressNeighborhood,
    string? AddressStreet,
    string? AddressPostalCode
) : ICommand;
