using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Maintenance;

namespace PropertyOS.Application.Maintenance.Commands.CreateMaintenanceRequest;

public class CreateMaintenanceRequestCommandHandler : IRequestHandler<CreateMaintenanceRequestCommand, Guid>
{
    private readonly IMaintenanceRequestRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public CreateMaintenanceRequestCommandHandler(
        IMaintenanceRequestRepository repository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Guid> Handle(CreateMaintenanceRequestCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId
            ?? throw new InvalidOperationException("Tenant context is required.");

        // ── Reference existence guards (security: never trust client IDs) ──────

        var buildingExists = await _repository.BuildingExistsAsync(request.BuildingId, companyId, cancellationToken);
        if (!buildingExists)
            throw new KeyNotFoundException($"Building '{request.BuildingId}' was not found.");

        if (request.ApartmentId.HasValue)
        {
            var apartmentExists = await _repository.ApartmentExistsAsync(request.ApartmentId.Value, companyId, cancellationToken);
            if (!apartmentExists)
                throw new KeyNotFoundException($"Apartment '{request.ApartmentId.Value}' was not found.");
        }

        if (request.TenantId.HasValue)
        {
            var tenantExists = await _repository.TenantExistsAsync(request.TenantId.Value, companyId, cancellationToken);
            if (!tenantExists)
                throw new KeyNotFoundException($"Tenant '{request.TenantId.Value}' was not found.");
        }

        // ── Aggregate creation (domain enforces all remaining invariants) ─────

        var maintenanceRequest = MaintenanceRequest.Create(
            companyId: companyId,
            buildingId: request.BuildingId,
            apartmentId: request.ApartmentId,
            tenantId: request.TenantId,
            title: request.Title,
            description: request.Description,
            category: request.Category,
            priority: request.Priority,
            requestDate: request.RequestDate,
            now: DateTimeOffset.UtcNow,
            createdBy: _currentUserContext.UserId);

        await _repository.AddAsync(maintenanceRequest, cancellationToken);

        // Persistence is owned by TransactionBehavior — SaveChangesAsync not called here.
        return maintenanceRequest.Id;
    }
}
