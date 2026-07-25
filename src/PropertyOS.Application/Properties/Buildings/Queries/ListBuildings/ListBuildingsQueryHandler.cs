using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;
using PropertyOS.Application.Properties.Buildings.Queries.Common;
using PropertyOS.Domain.Properties.Enums;

namespace PropertyOS.Application.Properties.Buildings.Queries.ListBuildings;

public class ListBuildingsQueryHandler : IRequestHandler<ListBuildingsQuery, List<BuildingDto>>
{
    private readonly IBuildingRepository _buildingRepository;
    private readonly ITenantContext _tenantContext;

    public ListBuildingsQueryHandler(IBuildingRepository buildingRepository, ITenantContext tenantContext)
    {
        _buildingRepository = buildingRepository;
        _tenantContext = tenantContext;
    }

    public async Task<List<BuildingDto>> Handle(ListBuildingsQuery request, CancellationToken cancellationToken)
    {
        if (_tenantContext.CompanyId == null)
            throw new UnauthorizedAccessException("Tenant context is missing or invalid.");

        var buildings = await _buildingRepository.ListByCompanyIdAsync(_tenantContext.CompanyId.Value, cancellationToken);

        // BuildingRepository.ListByCompanyIdAsync only returns active buildings per Phase 1 design.
        return buildings.Select(building => new BuildingDto
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
        }).ToList();
    }
}
