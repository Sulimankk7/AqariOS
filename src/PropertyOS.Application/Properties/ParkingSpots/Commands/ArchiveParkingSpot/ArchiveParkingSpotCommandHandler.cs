using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;
using PropertyOS.Application.Properties.ParkingSpots.Services;

namespace PropertyOS.Application.Properties.ParkingSpots.Commands.ArchiveParkingSpot;

public class ArchiveParkingSpotCommandHandler : IRequestHandler<ArchiveParkingSpotCommand, Unit>
{
    private readonly IParkingSpotRepository _parkingSpotRepository;
    private readonly IParkingSpotArchiveDependencyChecker _dependencyChecker;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ILogger<ArchiveParkingSpotCommandHandler> _logger;

    public ArchiveParkingSpotCommandHandler(
        IParkingSpotRepository parkingSpotRepository,
        IParkingSpotArchiveDependencyChecker dependencyChecker,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        ILogger<ArchiveParkingSpotCommandHandler> logger)
    {
        _parkingSpotRepository = parkingSpotRepository;
        _dependencyChecker = dependencyChecker;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
        _logger = logger;
    }

    public async Task<Unit> Handle(ArchiveParkingSpotCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("ArchiveParkingSpotCommand received for ParkingSpotId={ParkingSpotId}", request.Id);

        if (_tenantContext.CompanyId == null)
        {
            _logger.LogInformation("Archive failed for ParkingSpotId={ParkingSpotId}: Tenant context missing.", request.Id);
            throw new UnauthorizedAccessException("Tenant context is missing or invalid.");
        }

        var companyId = _tenantContext.CompanyId.Value;

        var spot = await _parkingSpotRepository.GetByIdAsync(request.Id, cancellationToken);
        if (spot == null || spot.CompanyId != companyId)
        {
            _logger.LogInformation("Archive failed: ParkingSpotId={ParkingSpotId} was not found or belongs to another company.", request.Id);
            throw new NotFoundException($"Parking spot with ID {request.Id} was not found.");
        }

        _logger.LogInformation("Entity found for ParkingSpotId={ParkingSpotId} SpotCode={SpotCode}. Checking active dependencies.", request.Id, spot.SpotCode);

        var dependencies = await _dependencyChecker.GetActiveDependenciesAsync(spot.Id, cancellationToken);
        if (dependencies.Count > 0)
        {
            _logger.LogInformation(
                "Archive blocked for ParkingSpotId={ParkingSpotId}: {DependencyCount} active dependency group(s) found.",
                request.Id, dependencies.Count);

            throw new ArchiveBlockedException("ParkingSpot", spot.SpotCode, dependencies);
        }

        try
        {
            spot.SoftDelete(DateTimeOffset.UtcNow, _currentUserContext.UserId);
            _logger.LogInformation("Archive succeeded for ParkingSpotId={ParkingSpotId}.", request.Id);
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "Archive failed for ParkingSpotId={ParkingSpotId} during soft delete.", request.Id);
            throw;
        }

        return Unit.Value;
    }
}
