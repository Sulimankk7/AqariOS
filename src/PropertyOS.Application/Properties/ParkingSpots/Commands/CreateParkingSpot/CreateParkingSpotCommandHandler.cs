using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;
using PropertyOS.Domain.Properties;

namespace PropertyOS.Application.Properties.ParkingSpots.Commands.CreateParkingSpot;

public class CreateParkingSpotCommandHandler : IRequestHandler<CreateParkingSpotCommand, Unit>
{
    private readonly IParkingSpotRepository _parkingSpotRepository;
    private readonly IBuildingRepository _buildingRepository;
    private readonly IApartmentRepository _apartmentRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public CreateParkingSpotCommandHandler(
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

    public async Task<Unit> Handle(CreateParkingSpotCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.CompanyId == null)
            throw new UnauthorizedAccessException("Tenant context is missing or invalid.");

        var companyId = _tenantContext.CompanyId.Value;

        var building = await _buildingRepository.GetByIdAsync(request.BuildingId, cancellationToken);
        if (building == null || building.CompanyId != companyId || !building.IsActive)
            throw new NotFoundException($"Building with ID {request.BuildingId} was not found.");

        if (request.DefaultApartmentId.HasValue)
        {
            var apartment = await _apartmentRepository.GetByIdAsync(request.DefaultApartmentId.Value, cancellationToken);
            if (apartment == null || apartment.CompanyId != companyId || apartment.BuildingId != building.Id)
                throw new NotFoundException($"Default apartment with ID {request.DefaultApartmentId.Value} was not found in this building.");
        }

        if (await _parkingSpotRepository.ExistsBySpotCodeAsync(building.Id, request.SpotCode, cancellationToken))
            throw new ConflictException($"A parking spot with code '{request.SpotCode}' already exists in this building.");

        var now = DateTimeOffset.UtcNow;
        var userId = _currentUserContext.UserId;

        var spot = ParkingSpot.Create(
            companyId: companyId,
            buildingId: building.Id,
            spotCode: request.SpotCode,
            createdAt: now,
            createdBy: userId,
            parkingType: request.ParkingType,
            defaultApartmentId: request.DefaultApartmentId,
            locationDescription: request.LocationDescription
        );

        await _parkingSpotRepository.AddAsync(spot, cancellationToken);

        return Unit.Value;
    }
}
