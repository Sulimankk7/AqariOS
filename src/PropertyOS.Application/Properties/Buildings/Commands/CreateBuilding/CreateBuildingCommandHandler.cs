using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;
using PropertyOS.Domain.Properties;

namespace PropertyOS.Application.Properties.Buildings.Commands.CreateBuilding;

public class CreateBuildingCommandHandler : IRequestHandler<CreateBuildingCommand, Unit>
{
    private readonly IBuildingRepository _buildingRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public CreateBuildingCommandHandler(
        IBuildingRepository buildingRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _buildingRepository = buildingRepository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(CreateBuildingCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.CompanyId == null)
            throw new UnauthorizedAccessException("Tenant context is missing or invalid.");

        var companyId = _tenantContext.CompanyId.Value;

        if (!string.IsNullOrWhiteSpace(request.InternalCode))
        {
            if (await _buildingRepository.ExistsByCodeAsync(companyId, request.InternalCode, cancellationToken))
                throw new PropertyOS.Application.Common.Exceptions.ConflictException($"A building with the code '{request.InternalCode}' already exists.");
        }

        // Note: Integration gap reported: ISubscriptionService doesn't expose MaxBuildings limit for company directly.
        // Subscription limits are deferred.

        var buildingId = Guid.CreateVersion7();
        var now = DateTimeOffset.UtcNow;
        var userId = _currentUserContext.UserId;

        var building = Building.Create(
            companyId: companyId,
            name: request.Name,
            totalFloors: request.TotalFloors,
            createdAt: now,
            createdBy: userId,
            buildingType: request.BuildingType,
            internalCode: request.InternalCode,
            constructionYear: request.ConstructionYear,
            gpsLatitude: request.GpsLatitude,
            gpsLongitude: request.GpsLongitude,
            id: buildingId
        );

        var address = BuildingAddress.Create(
            buildingId: building.Id,
            companyId: companyId,
            governorate: request.AddressGovernorate,
            district: request.AddressCity,
            createdAt: now,
            area: request.AddressNeighborhood,
            streetName: request.AddressStreet,
            postalCode: request.AddressPostalCode
        );

        building.SetAddress(address);

        await _buildingRepository.AddAsync(building, cancellationToken);

        // Note: SaveChanges is owned by TransactionBehavior
        return Unit.Value;
    }
}
