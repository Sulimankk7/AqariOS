using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;
using PropertyOS.Application.Properties.Buildings.Queries.Common;
using PropertyOS.Domain.Properties.Enums;

namespace PropertyOS.Application.Properties.Buildings.Queries.GetBuildingById;

public class GetBuildingByIdQueryHandler : IRequestHandler<GetBuildingByIdQuery, BuildingDto?>
{
    private readonly IBuildingRepository _buildingRepository;
    private readonly ITenantContext _tenantContext;

    public GetBuildingByIdQueryHandler(IBuildingRepository buildingRepository, ITenantContext tenantContext)
    {
        _buildingRepository = buildingRepository;
        _tenantContext = tenantContext;
    }

    public async Task<BuildingDto?> Handle(GetBuildingByIdQuery request, CancellationToken cancellationToken)
    {
        if (_tenantContext.CompanyId == null)
            throw new UnauthorizedAccessException("Tenant context is missing or invalid.");

        var building = await _buildingRepository.GetByIdAsync(request.Id, cancellationToken);

        if (building == null)
            throw new PropertyOS.Application.Common.Exceptions.NotFoundException($"Building with ID {request.Id} was not found.");

        if (building.CompanyId != _tenantContext.CompanyId.Value)
            throw new PropertyOS.Application.Common.Exceptions.NotFoundException($"Building with ID {request.Id} was not found."); // DO NOT LEAK EXISTENCE!

        if (!building.IsActive)
            throw new PropertyOS.Application.Common.Exceptions.NotFoundException($"Building with ID {request.Id} was not found.");

        return new BuildingDto
        {
            Id = building.Id,
            CompanyId = building.CompanyId,
            Name = building.Name,
            InternalCode = building.InternalCode,
            BuildingType = building.BuildingType,
            TotalFloors = building.TotalFloors,
            ConstructionYear = building.ConstructionYear,
            GpsLatitude = building.GpsLatitude,
            GpsLongitude = building.GpsLongitude,
            TotalApartmentsCount = building.TotalApartmentsCount,
            IsActive = building.IsActive,
            CreatedAt = building.CreatedAt,
            UpdatedAt = building.UpdatedAt,
            Address = building.Address == null ? null : new BuildingAddressDto
            {
                BuildingId = building.Address.BuildingId,
                Governorate = building.Address.Governorate,
                District = building.Address.District,
                Area = building.Address.Area,
                StreetName = building.Address.StreetName,
                PostalCode = building.Address.PostalCode
            }
        };
    }
}
