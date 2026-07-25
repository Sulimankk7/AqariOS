using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;

namespace PropertyOS.Application.Properties.Floors.Commands.UpdateFloor;

public class UpdateFloorCommandHandler : IRequestHandler<UpdateFloorCommand, Unit>
{
    private readonly IFloorRepository _floorRepository;
    private readonly IBuildingRepository _buildingRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public UpdateFloorCommandHandler(
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

    public async Task<Unit> Handle(UpdateFloorCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.CompanyId == null)
            throw new UnauthorizedAccessException("Tenant context is missing or invalid.");

        var companyId = _tenantContext.CompanyId.Value;

        var floor = await _floorRepository.GetByIdAsync(request.Id, cancellationToken);
        if (floor == null || floor.CompanyId != companyId || floor.DeletedAt != null)
            throw new NotFoundException($"Floor with ID {request.Id} was not found.");

        var building = await _buildingRepository.GetByIdAsync(floor.BuildingId, cancellationToken);
        if (building == null || !building.IsActive)
            throw new BusinessRuleException("Cannot update a floor of an inactive building.");

        var now = DateTimeOffset.UtcNow;
        var userId = _currentUserContext.UserId;

        floor.UpdateLabel(
            floorLabel: request.FloorLabel,
            floorType: request.FloorType,
            updatedAt: now,
            updatedBy: userId
        );

        return Unit.Value;
    }
}
