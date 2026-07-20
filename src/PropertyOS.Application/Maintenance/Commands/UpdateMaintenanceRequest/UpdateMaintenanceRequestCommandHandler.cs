using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Maintenance.Commands.UpdateMaintenanceRequest;

public class UpdateMaintenanceRequestCommandHandler : IRequestHandler<UpdateMaintenanceRequestCommand, MediatR.Unit>
{
    private readonly IMaintenanceRequestRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public UpdateMaintenanceRequestCommandHandler(
        IMaintenanceRequestRepository repository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<MediatR.Unit> Handle(UpdateMaintenanceRequestCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId
            ?? throw new InvalidOperationException("Tenant context is required.");

        // Load aggregate (RLS ensures cross-tenant requests return null)
        var maintenanceRequest = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Maintenance request '{request.Id}' was not found.");

        // Guard: cannot update a soft-deleted request (domain also guards, belt-and-suspenders here)
        if (maintenanceRequest.DeletedAt != null)
            throw new InvalidOperationException("Cannot modify a deleted maintenance request.");

        // Reference existence guards for updated FKs
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

        maintenanceRequest.UpdateDetails(
            buildingId: request.BuildingId,
            apartmentId: request.ApartmentId,
            tenantId: request.TenantId,
            title: request.Title,
            description: request.Description,
            category: request.Category,
            priority: request.Priority,
            internalNotes: request.InternalNotes,
            updatedAt: DateTimeOffset.UtcNow,
            updatedBy: _currentUserContext.UserId);

        return MediatR.Unit.Value;
    }
}
