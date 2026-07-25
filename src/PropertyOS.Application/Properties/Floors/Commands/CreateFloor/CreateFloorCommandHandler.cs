using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;
using PropertyOS.Domain.Properties;

namespace PropertyOS.Application.Properties.Floors.Commands.CreateFloor;

public class CreateFloorCommandHandler : IRequestHandler<CreateFloorCommand, Unit>
{
    private readonly IFloorRepository _floorRepository;
    private readonly IBuildingRepository _buildingRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public CreateFloorCommandHandler(
        IFloorRepository floorRepository,
        IBuildingRepository buildingRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _floorRepository = floorRepository;
        _buildingRepository = buildingRepository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(CreateFloorCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.CompanyId == null)
            throw new UnauthorizedAccessException("Tenant context is missing or invalid.");

        var companyId = _tenantContext.CompanyId.Value;

        var building = await _buildingRepository.GetByIdAsync(request.BuildingId, cancellationToken);
        if (building == null || building.CompanyId != companyId)
            throw new NotFoundException($"Building with ID {request.BuildingId} was not found.");

        if (!building.IsActive)
            throw new BusinessRuleException("Cannot add a floor to an inactive building.");

        if (await _floorRepository.ExistsByFloorNumberAsync(request.BuildingId, request.FloorNumber, cancellationToken))
            throw new ConflictException($"A floor with number {request.FloorNumber} already exists in this building.");

        var now = DateTimeOffset.UtcNow;
        var userId = _currentUserContext.UserId;

        var floor = Floor.Create(
            companyId: companyId,
            buildingId: request.BuildingId,
            floorNumber: request.FloorNumber,
            floorLabel: request.FloorLabel,
            createdAt: now,
            createdBy: userId,
            floorType: request.FloorType
        );

        await _floorRepository.AddAsync(floor, cancellationToken);

        return Unit.Value;
    }
}
