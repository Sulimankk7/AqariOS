using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Properties;
using PropertyOS.Application.Properties.Apartments.Services;

namespace PropertyOS.Application.Properties.Apartments.Commands.ArchiveApartment;

public class ArchiveApartmentCommandHandler : IRequestHandler<ArchiveApartmentCommand, Unit>
{
    private readonly IApartmentRepository _apartmentRepository;
    private readonly IApartmentArchiveDependencyChecker _dependencyChecker;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ILogger<ArchiveApartmentCommandHandler> _logger;

    public ArchiveApartmentCommandHandler(
        IApartmentRepository apartmentRepository,
        IApartmentArchiveDependencyChecker dependencyChecker,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        ILogger<ArchiveApartmentCommandHandler> logger)
    {
        _apartmentRepository = apartmentRepository;
        _dependencyChecker = dependencyChecker;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
        _logger = logger;
    }

    public async Task<Unit> Handle(ArchiveApartmentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("ArchiveApartmentCommand received for ApartmentId={ApartmentId}", request.Id);

        if (_tenantContext.CompanyId == null)
        {
            _logger.LogInformation("Archive failed for ApartmentId={ApartmentId}: Tenant context missing.", request.Id);
            throw new UnauthorizedAccessException("Tenant context is missing or invalid.");
        }

        var companyId = _tenantContext.CompanyId.Value;

        var apartment = await _apartmentRepository.GetByIdAsync(request.Id, cancellationToken);
        if (apartment == null || apartment.CompanyId != companyId)
        {
            _logger.LogInformation("Archive failed: ApartmentId={ApartmentId} was not found or belongs to another company.", request.Id);
            throw new NotFoundException($"Apartment with ID {request.Id} was not found.");
        }

        _logger.LogInformation("Entity found for ApartmentId={ApartmentId} UnitNumber={UnitNumber}. Checking active dependencies.", request.Id, apartment.UnitNumber);

        var dependencies = await _dependencyChecker.GetActiveDependenciesAsync(apartment.Id, cancellationToken);
        if (dependencies.Count > 0)
        {
            _logger.LogInformation(
                "Archive blocked for ApartmentId={ApartmentId}: {DependencyCount} active dependency group(s) found.",
                request.Id, dependencies.Count);

            throw new ArchiveBlockedException("Apartment", apartment.UnitNumber, dependencies);
        }

        try
        {
            apartment.SoftDelete(DateTimeOffset.UtcNow, _currentUserContext.UserId);
            _logger.LogInformation("Archive succeeded for ApartmentId={ApartmentId}.", request.Id);
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "Archive failed for ApartmentId={ApartmentId} during soft delete.", request.Id);
            throw;
        }

        return Unit.Value;
    }
}
