using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Maintenance.Commands.UpdateMaintenanceRequestStatus;

public class UpdateMaintenanceRequestStatusCommandHandler
    : IRequestHandler<UpdateMaintenanceRequestStatusCommand, MediatR.Unit>
{
    private readonly IMaintenanceRequestRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public UpdateMaintenanceRequestStatusCommandHandler(
        IMaintenanceRequestRepository repository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<MediatR.Unit> Handle(
        UpdateMaintenanceRequestStatusCommand request,
        CancellationToken cancellationToken)
    {
        _ = _tenantContext.CompanyId
            ?? throw new InvalidOperationException("Tenant context is required.");

        var maintenanceRequest = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Maintenance request '{request.Id}' was not found.");

        var now = DateTimeOffset.UtcNow;

        // Domain aggregate validates transition, creates history entry, raises event.
        // Any invalid transition throws InvalidOperationException — no transition logic here.
        var history = maintenanceRequest.UpdateStatus(
            newStatus: request.NewStatus,
            changedAt: now,
            changedBy: _currentUserContext.UserId,
            reason: request.Reason);

        // History must be persisted in the same Unit-of-Work as the aggregate root update.
        await _repository.AddStatusHistoryAsync(history, cancellationToken);

        return MediatR.Unit.Value;
    }
}
