using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;
using PropertyOS.Application.Properties.Floors.Services;

namespace PropertyOS.Application.Properties.Floors.Commands.ArchiveFloor;

public class ArchiveFloorCommandHandler : IRequestHandler<ArchiveFloorCommand, Unit>
{
    private readonly IFloorRepository _floorRepository;
    private readonly IFloorArchiveDependencyChecker _dependencyChecker;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ILogger<ArchiveFloorCommandHandler> _logger;

    public ArchiveFloorCommandHandler(
        IFloorRepository floorRepository,
        IFloorArchiveDependencyChecker dependencyChecker,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        ILogger<ArchiveFloorCommandHandler> logger)
    {
        _floorRepository = floorRepository;
        _dependencyChecker = dependencyChecker;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
        _logger = logger;
    }

    public async Task<Unit> Handle(ArchiveFloorCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("ArchiveFloorCommand received for FloorId={FloorId}", request.Id);

        if (_tenantContext.CompanyId == null)
        {
            _logger.LogInformation("Archive failed for FloorId={FloorId}: Tenant context missing.", request.Id);
            throw new UnauthorizedAccessException("Tenant context is missing or invalid.");
        }

        var companyId = _tenantContext.CompanyId.Value;

        var floor = await _floorRepository.GetByIdAsync(request.Id, cancellationToken);
        if (floor == null || floor.CompanyId != companyId)
        {
            _logger.LogInformation("Archive failed: FloorId={FloorId} was not found or belongs to another company.", request.Id);
            throw new NotFoundException($"Floor with ID {request.Id} was not found.");
        }

        _logger.LogInformation("Entity found for FloorId={FloorId}. Checking active dependencies.", request.Id);

        var dependencies = await _dependencyChecker.GetActiveDependenciesAsync(floor.Id, cancellationToken);
        if (dependencies.Count > 0)
        {
            _logger.LogInformation(
                "Archive blocked for FloorId={FloorId}: {DependencyCount} active dependency group(s) found.",
                request.Id, dependencies.Count);

            throw new ArchiveBlockedException("Floor", floor.FloorLabel, dependencies);
        }

        try
        {
            floor.SoftDelete(DateTimeOffset.UtcNow, _currentUserContext.UserId);
            _logger.LogInformation("Archive succeeded for FloorId={FloorId}.", request.Id);
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "Archive failed for FloorId={FloorId} during soft delete.", request.Id);
            throw;
        }

        return Unit.Value;
    }
}
