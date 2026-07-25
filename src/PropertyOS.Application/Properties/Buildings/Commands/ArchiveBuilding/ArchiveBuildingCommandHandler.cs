using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;
using PropertyOS.Domain.Properties;

namespace PropertyOS.Application.Properties.Buildings.Commands.ArchiveBuilding;

public class ArchiveBuildingCommandHandler : IRequestHandler<ArchiveBuildingCommand, Unit>
{
    private readonly IBuildingRepository _buildingRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public ArchiveBuildingCommandHandler(
        IBuildingRepository buildingRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _buildingRepository = buildingRepository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(ArchiveBuildingCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.CompanyId == null)
            throw new UnauthorizedAccessException("Tenant context is missing or invalid.");

        var companyId = _tenantContext.CompanyId.Value;

        var building = await _buildingRepository.GetByIdAsync(request.Id, cancellationToken);

        if (building == null)
            throw new PropertyOS.Application.Common.Exceptions.NotFoundException($"Building with ID {request.Id} was not found.");

        if (building.CompanyId != companyId)
            throw new PropertyOS.Application.Common.Exceptions.NotFoundException($"Building with ID {request.Id} was not found."); // Do not leak existence

        // The building soft-delete semantics are just calling SoftDelete.
        // According to PropertyOS_Module4_Properties.md, Soft Delete Strategy: Standard.
        // A soft-deleted building's floors/apartments/leases/payments all remain intact and queryable for statutory retention.
        // Dependency rule restrictions (e.g., active leases preventing delete) are deferred to Module 5 as requested by the prompt.

        building.SoftDelete(DateTimeOffset.UtcNow, _currentUserContext.UserId);

        // Note: SaveChanges is owned by TransactionBehavior
        return Unit.Value;
    }
}
