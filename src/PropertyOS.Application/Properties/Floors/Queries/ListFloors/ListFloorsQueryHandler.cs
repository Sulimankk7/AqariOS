using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;
using PropertyOS.Application.Properties.Floors.Queries.Common;

namespace PropertyOS.Application.Properties.Floors.Queries.ListFloors;

public class ListFloorsQueryHandler : IRequestHandler<ListFloorsQuery, List<FloorDto>>
{
    private readonly IFloorRepository _floorRepository;
    private readonly IBuildingRepository _buildingRepository;
    private readonly ITenantContext _tenantContext;

    public ListFloorsQueryHandler(
        IFloorRepository floorRepository,
        IBuildingRepository buildingRepository,
        ITenantContext tenantContext)
    {
        _floorRepository = floorRepository;
        _buildingRepository = buildingRepository;
        _tenantContext = tenantContext;
    }

    public async Task<List<FloorDto>> Handle(ListFloorsQuery request, CancellationToken cancellationToken)
    {
        if (_tenantContext.CompanyId == null)
            throw new UnauthorizedAccessException("Tenant context is missing or invalid.");

        var companyId = _tenantContext.CompanyId.Value;

        var building = await _buildingRepository.GetByIdAsync(request.BuildingId, cancellationToken);
        if (building == null || building.CompanyId != companyId || !building.IsActive)
            throw new NotFoundException($"Building with ID {request.BuildingId} was not found.");

        var floors = await _floorRepository.ListByBuildingIdAsync(request.BuildingId, cancellationToken);

        return floors.Select(floor => new FloorDto
        {
            Id = floor.Id,
            CompanyId = floor.CompanyId,
            BuildingId = floor.BuildingId,
            FloorNumber = floor.FloorNumber,
            FloorLabel = floor.FloorLabel,
            FloorType = floor.FloorType,
            ApartmentsCount = floor.ApartmentsCount,
            CreatedAt = floor.CreatedAt,
            UpdatedAt = floor.UpdatedAt
        }).ToList();
    }
}
