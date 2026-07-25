using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;
using PropertyOS.Application.Properties.ParkingSpots.Queries.Common;

namespace PropertyOS.Application.Properties.ParkingSpots.Queries.GetParkingSpotById;

public class GetParkingSpotByIdQueryHandler : IRequestHandler<GetParkingSpotByIdQuery, ParkingSpotDto?>
{
    private readonly IParkingSpotRepository _parkingSpotRepository;
    private readonly ITenantContext _tenantContext;

    public GetParkingSpotByIdQueryHandler(IParkingSpotRepository parkingSpotRepository, ITenantContext tenantContext)
    {
        _parkingSpotRepository = parkingSpotRepository;
        _tenantContext = tenantContext;
    }

    public async Task<ParkingSpotDto?> Handle(GetParkingSpotByIdQuery request, CancellationToken cancellationToken)
    {
        if (_tenantContext.CompanyId == null)
            throw new UnauthorizedAccessException("Tenant context is missing or invalid.");

        var companyId = _tenantContext.CompanyId.Value;

        var spot = await _parkingSpotRepository.GetByIdAsync(request.Id, cancellationToken);
        if (spot == null || spot.CompanyId != companyId || spot.DeletedAt != null)
            throw new NotFoundException($"Parking spot with ID {request.Id} was not found.");

        return new ParkingSpotDto
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
        };
    }
}
