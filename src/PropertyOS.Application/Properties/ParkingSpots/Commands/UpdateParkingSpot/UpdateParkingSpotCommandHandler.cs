using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;

namespace PropertyOS.Application.Properties.ParkingSpots.Commands.UpdateParkingSpot;

public class UpdateParkingSpotCommandHandler : IRequestHandler<UpdateParkingSpotCommand, Unit>
{
    private readonly IParkingSpotRepository _parkingSpotRepository;
    private readonly IBuildingRepository _buildingRepository;
    private readonly IApartmentRepository _apartmentRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public UpdateParkingSpotCommandHandler(
        IParkingSpotRepository parkingSpotRepository,
        IBuildingRepository buildingRepository,
        IApartmentRepository apartmentRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _parkingSpotRepository = parkingSpotRepository;
        _buildingRepository = buildingRepository;
        _apartmentRepository = apartmentRepository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(UpdateParkingSpotCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.CompanyId == null)
            throw new UnauthorizedAccessException("Tenant context is missing or invalid.");

        var companyId = _tenantContext.CompanyId.Value;

        var spot = await _parkingSpotRepository.GetByIdAsync(request.Id, cancellationToken);
        if (spot == null || spot.CompanyId != companyId || spot.DeletedAt != null)
            throw new NotFoundException($"Parking spot with ID {request.Id} was not found.");

        var building = await _buildingRepository.GetByIdAsync(spot.BuildingId, cancellationToken);
        if (building == null || !building.IsActive)
            throw new BusinessRuleException("Cannot update a parking spot of an inactive building.");

        if (!string.Equals(spot.SpotCode, request.SpotCode.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            if (await _parkingSpotRepository.ExistsBySpotCodeAsync(spot.BuildingId, request.SpotCode, cancellationToken))
                throw new ConflictException($"A parking spot with code '{request.SpotCode}' already exists in this building.");
        }

        if (request.DefaultApartmentId.HasValue)
        {
            var apartment = await _apartmentRepository.GetByIdAsync(request.DefaultApartmentId.Value, cancellationToken);
            if (apartment == null || apartment.CompanyId != companyId || apartment.BuildingId != spot.BuildingId)
                throw new NotFoundException($"Default apartment with ID {request.DefaultApartmentId.Value} was not found in this building.");
        }

        var now = DateTimeOffset.UtcNow;
        var userId = _currentUserContext.UserId;

        spot.UpdateDetails(
            spotCode: request.SpotCode,
            parkingType: request.ParkingType,
            locationDescription: request.LocationDescription,
            defaultApartmentId: request.DefaultApartmentId,
            updatedAt: now,
            updatedBy: userId
        );

        return Unit.Value;
    }
}
