using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;

namespace PropertyOS.Application.Properties.Floors.Commands.ArchiveFloor;

public class ArchiveFloorCommandHandler : IRequestHandler<ArchiveFloorCommand, Unit>
{
    private readonly IFloorRepository _floorRepository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public ArchiveFloorCommandHandler(
        IFloorRepository floorRepository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _floorRepository = floorRepository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(ArchiveFloorCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.CompanyId == null)
            throw new UnauthorizedAccessException("Tenant context is missing or invalid.");

        var companyId = _tenantContext.CompanyId.Value;

        var floor = await _floorRepository.GetByIdAsync(request.Id, cancellationToken);
        if (floor == null || floor.CompanyId != companyId)
            throw new NotFoundException($"Floor with ID {request.Id} was not found.");

        floor.SoftDelete(DateTimeOffset.UtcNow, _currentUserContext.UserId);

        return Unit.Value;
    }
}
