using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;
using PropertyOS.Domain.Properties;

namespace PropertyOS.Application.Properties.Buildings.Commands.UpdateBuilding;

public class UpdateBuildingCommandHandler : IRequestHandler<UpdateBuildingCommand, Unit>
{
    private readonly IBuildingRepository _buildingRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public UpdateBuildingCommandHandler(
        IBuildingRepository buildingRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _buildingRepository = buildingRepository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(UpdateBuildingCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.CompanyId == null)
            throw new UnauthorizedAccessException("Tenant context is missing or invalid.");

        var companyId = _tenantContext.CompanyId.Value;

        var building = await _buildingRepository.GetByIdAsync(request.Id, cancellationToken);

        if (building == null)
            throw new PropertyOS.Application.Common.Exceptions.NotFoundException($"Building with ID {request.Id} was not found.");

        if (building.CompanyId != companyId)
            throw new PropertyOS.Application.Common.Exceptions.NotFoundException($"Building with ID {request.Id} was not found."); // Do not leak existence

        if (!building.IsActive)
            throw new PropertyOS.Application.Common.Exceptions.BusinessRuleException("Cannot update an inactive building.");

        // Uniqueness checks
        if (!string.IsNullOrWhiteSpace(request.InternalCode) && building.InternalCode != request.InternalCode)
        {
            if (await _buildingRepository.ExistsByCodeAsync(companyId, request.InternalCode, cancellationToken))
                throw new PropertyOS.Application.Common.Exceptions.ConflictException($"A building with the code '{request.InternalCode}' already exists.");
        }

        var now = DateTimeOffset.UtcNow;
        var userId = _currentUserContext.UserId;

        building.UpdateDetails(
            name: request.Name,
            internalCode: request.InternalCode,
            buildingType: request.BuildingType,
            constructionYear: request.ConstructionYear,
            gpsLatitude: request.GpsLatitude,
            gpsLongitude: request.GpsLongitude,
            updatedAt: now,
            updatedBy: userId
        );

        if (building.Address != null)
        {
            building.Address.UpdateAddress(
                governorate: request.AddressGovernorate,
                district: request.AddressCity, // Assuming City maps to District in command structure for simplicity as per prior logic
                area: request.AddressNeighborhood,
                streetName: request.AddressStreet,
                buildingPlateNumber: null, // Command didn't have this
                nearestLandmark: null, // Command didn't have this
                postalCode: request.AddressPostalCode,
                fullAddressText: null, // Handled by formatting logic later
                updatedAt: now
            );
        }
        else
        {
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
        }

        // Note: SaveChanges is owned by TransactionBehavior
        return Unit.Value;
    }
}
