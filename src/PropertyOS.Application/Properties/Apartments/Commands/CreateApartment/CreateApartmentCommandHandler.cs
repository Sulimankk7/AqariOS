using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;
using PropertyOS.Domain.Properties;
using PropertyOS.Domain.Properties.Enums;

namespace PropertyOS.Application.Properties.Apartments.Commands.CreateApartment;

public class CreateApartmentCommandHandler : IRequestHandler<CreateApartmentCommand, Unit>
{
    private readonly IApartmentRepository _apartmentRepository;
    private readonly IFloorRepository _floorRepository;
    private readonly IBuildingRepository _buildingRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public CreateApartmentCommandHandler(
        IApartmentRepository apartmentRepository,
        IFloorRepository floorRepository,
        IBuildingRepository buildingRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _apartmentRepository = apartmentRepository;
        _floorRepository = floorRepository;
        _buildingRepository = buildingRepository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(CreateApartmentCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.CompanyId == null)
            throw new UnauthorizedAccessException("Tenant context is missing or invalid.");

        var companyId = _tenantContext.CompanyId.Value;

        var floor = await _floorRepository.GetByIdAsync(request.FloorId, cancellationToken);
        if (floor == null || floor.CompanyId != companyId || floor.DeletedAt != null)
            throw new NotFoundException($"Floor with ID {request.FloorId} was not found.");

        var building = await _buildingRepository.GetByIdAsync(floor.BuildingId, cancellationToken);
        if (building == null || building.CompanyId != companyId || !building.IsActive)
            throw new BusinessRuleException("Cannot add an apartment to an inactive or non-existent building.");

        if (floor.BuildingId != building.Id)
            throw new BusinessRuleException($"Floor with ID {request.FloorId} does not belong to Building {building.Id}.");

        if (await _apartmentRepository.UnitNumberExistsInBuildingAsync(building.Id, request.UnitNumber, cancellationToken))
            throw new ConflictException($"An apartment with unit number '{request.UnitNumber}' already exists in this building.");

        var now = DateTimeOffset.UtcNow;
        var userId = _currentUserContext.UserId;

        // Enforce null external owner fields if OwnershipStatus is not ThirdPartyOwned
        var isThirdParty = request.OwnershipStatus == OwnershipStatus.ThirdPartyOwned;
        var externalOwnerName = isThirdParty ? request.ExternalOwnerName : null;
        var externalOwnerPhone = isThirdParty ? request.ExternalOwnerPhone : null;

        var apartment = Apartment.Create(
            companyId: companyId,
            buildingId: building.Id,
            floorId: floor.Id,
            unitNumber: request.UnitNumber,
            areaSqm: request.AreaSqm,
            createdAt: now,
            createdBy: userId,
            ownershipStatus: request.OwnershipStatus,
            externalOwnerName: externalOwnerName,
            externalOwnerPhone: externalOwnerPhone,
            bedrooms: request.Bedrooms,
            bathrooms: request.Bathrooms,
            baseRentAmount: request.BaseRentAmount,
            baseRentCurrency: request.BaseRentCurrency
        );

        await _apartmentRepository.AddAsync(apartment, cancellationToken);

        return Unit.Value;
    }
}
