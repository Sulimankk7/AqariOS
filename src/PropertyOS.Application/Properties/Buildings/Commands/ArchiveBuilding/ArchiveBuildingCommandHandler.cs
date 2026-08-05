using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;
using PropertyOS.Application.Properties.Buildings.Services;

namespace PropertyOS.Application.Properties.Buildings.Commands.ArchiveBuilding;

public class ArchiveBuildingCommandHandler : IRequestHandler<ArchiveBuildingCommand, Unit>
{
    private readonly IBuildingRepository _buildingRepository;
    private readonly IBuildingArchiveDependencyChecker _dependencyChecker;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ILogger<ArchiveBuildingCommandHandler> _logger;

    public ArchiveBuildingCommandHandler(
        IBuildingRepository buildingRepository,
        IBuildingArchiveDependencyChecker dependencyChecker,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        ILogger<ArchiveBuildingCommandHandler> logger)
    {
        _buildingRepository = buildingRepository;
        _dependencyChecker = dependencyChecker;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
        _logger = logger;
    }

    public async Task<Unit> Handle(ArchiveBuildingCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("ArchiveBuildingCommand received for BuildingId={BuildingId}", request.Id);

        if (_tenantContext.CompanyId == null)
        {
            _logger.LogInformation("Archive failed for BuildingId={BuildingId}: Tenant context missing.", request.Id);
            throw new UnauthorizedAccessException("Tenant context is missing or invalid.");
        }

        var companyId = _tenantContext.CompanyId.Value;

        var building = await _buildingRepository.GetByIdAsync(request.Id, cancellationToken);
        if (building == null || building.CompanyId != companyId)
        {
            _logger.LogInformation("Archive failed: BuildingId={BuildingId} was not found or belongs to another company.", request.Id);
            throw new NotFoundException($"Building with ID {request.Id} was not found.");
        }

        _logger.LogInformation("Entity found for BuildingId={BuildingId} Name={BuildingName}. Checking active dependencies.", request.Id, building.Name);

        var dependencies = await _dependencyChecker.GetActiveDependenciesAsync(building.Id, cancellationToken);
        if (dependencies.Count > 0)
        {
            _logger.LogInformation(
                "Archive blocked for BuildingId={BuildingId}: {DependencyCount} active dependency group(s) found.",
                request.Id, dependencies.Count);

            throw new ArchiveBlockedException("Building", building.Name, dependencies);
        }

        try
        {
            building.SoftDelete(DateTimeOffset.UtcNow, _currentUserContext.UserId);
            _logger.LogInformation("Archive succeeded for BuildingId={BuildingId}.", request.Id);
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "Archive failed for BuildingId={BuildingId} during soft delete.", request.Id);
            throw;
        }

        return Unit.Value;
    }
}
