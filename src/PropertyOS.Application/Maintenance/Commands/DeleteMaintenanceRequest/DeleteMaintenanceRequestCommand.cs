using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Maintenance.Commands.DeleteMaintenanceRequest;

public record DeleteMaintenanceRequestCommand(Guid Id) : ICommand;

public class DeleteMaintenanceRequestCommandHandler
    : IRequestHandler<DeleteMaintenanceRequestCommand, MediatR.Unit>
{
    private readonly IMaintenanceRequestRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public DeleteMaintenanceRequestCommandHandler(
        IMaintenanceRequestRepository repository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<MediatR.Unit> Handle(
        DeleteMaintenanceRequestCommand request,
        CancellationToken cancellationToken)
    {
        _ = _tenantContext.CompanyId
            ?? throw new InvalidOperationException("Tenant context is required.");

        var maintenanceRequest = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Maintenance request '{request.Id}' was not found.");

        try
        {
            maintenanceRequest.SoftDelete(
                deletedAt: DateTimeOffset.UtcNow,
                deletedBy: _currentUserContext.UserId);
        }
        catch (InvalidOperationException ex)
        {
            // Domain rule: a maintenance request cannot be deleted twice
            throw new BusinessRuleException(ex.Message, "MAINTENANCE_REQUEST_ALREADY_DELETED");
        }

        return MediatR.Unit.Value;
    }
}
