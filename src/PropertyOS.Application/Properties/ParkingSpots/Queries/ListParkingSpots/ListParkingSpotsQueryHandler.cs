using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;
using PropertyOS.Application.Properties.ParkingSpots.Queries.Common;

namespace PropertyOS.Application.Properties.ParkingSpots.Queries.ListParkingSpots;

public class ListParkingSpotsQueryHandler : IRequestHandler<ListParkingSpotsQuery, List<ParkingSpotDto>>
{
    private readonly IParkingSpotRepository _parkingSpotRepository;
    private readonly IBuildingRepository _buildingRepository;
    private readonly ITenantContext _tenantContext;

    public ListParkingSpotsQueryHandler(
        IParkingSpotRepository parkingSpotRepository,
        IBuildingRepository buildingRepository,
        ITenantContext tenantContext)
    {
        _parkingSpotRepository = parkingSpotRepository;
        _buildingRepository = buildingRepository;
        _tenantContext = tenantContext;
    }

    public async Task<List<ParkingSpotDto>> Handle(ListParkingSpotsQuery request, CancellationToken cancellationToken)
    {
        if (_tenantContext.CompanyId == null)
            throw new UnauthorizedAccessException("Tenant context is missing or invalid.");

        var companyId = _tenantContext.CompanyId.Value;

        var building = await _buildingRepository.GetByIdAsync(request.BuildingId, cancellationToken);
        if (building == null || building.CompanyId != companyId || !building.IsActive)
            throw new NotFoundException($"Building with ID {request.BuildingId} was not found.");

        var spots = await _parkingSpotRepository.ListByBuildingIdAsync(request.BuildingId, cancellationToken);

        return spots.Select(spot => new ParkingSpotDto
        {
            Id = spot.Id,
            CompanyId = spot.CompanyId,
            BuildingId = spot.BuildingId,
            DefaultApartmentId = spot.DefaultApartmentId,
            SpotCode = spot.SpotCode,
            ParkingType = spot.ParkingType,
            LocationDescription = spot.LocationDescription,
            IsActive = spot.IsActive,
            CreatedAt = spot.CreatedAt,
            UpdatedAt = spot.UpdatedAt
        }).ToList();
    }
}
