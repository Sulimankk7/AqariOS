using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Maintenance.Commands.RemoveMaintenanceAttachment;

public record RemoveMaintenanceAttachmentCommand(Guid RequestId, Guid AttachmentId) : IRequest<MediatR.Unit>;

public class RemoveMaintenanceAttachmentCommandHandler
    : IRequestHandler<RemoveMaintenanceAttachmentCommand, MediatR.Unit>
{
    private readonly IMaintenanceRequestRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public RemoveMaintenanceAttachmentCommandHandler(
        IMaintenanceRequestRepository repository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<MediatR.Unit> Handle(
        RemoveMaintenanceAttachmentCommand request,
        CancellationToken cancellationToken)
    {
        _ = _tenantContext.CompanyId
            ?? throw new InvalidOperationException("Tenant context is required.");

        var maintenanceRequest = await _repository.GetByIdAsync(request.RequestId, cancellationToken)
            ?? throw new KeyNotFoundException($"Maintenance request '{request.RequestId}' was not found.");

        maintenanceRequest.RemoveAttachment(
            attachmentId: request.AttachmentId,
            now: DateTimeOffset.UtcNow,
            deletedBy: _currentUserContext.UserId);

        return MediatR.Unit.Value;
    }
}
