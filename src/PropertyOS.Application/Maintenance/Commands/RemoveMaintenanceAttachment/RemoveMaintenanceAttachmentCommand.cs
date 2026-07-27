using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Maintenance.Commands.RemoveMaintenanceAttachment;

public record RemoveMaintenanceAttachmentCommand(Guid RequestId, Guid AttachmentId) : ICommand;

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
            ?? throw new NotFoundException($"Maintenance request '{request.RequestId}' was not found.");

        try
        {
            maintenanceRequest.RemoveAttachment(
                attachmentId: request.AttachmentId,
                now: DateTimeOffset.UtcNow,
                deletedBy: _currentUserContext.UserId);
        }
        catch (KeyNotFoundException ex)
        {
            // Domain lookup: the attachment does not exist (or is already deleted) on this request
            throw new NotFoundException(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            // Domain rule: attachments cannot be removed from a deleted maintenance request
            throw new BusinessRuleException(ex.Message, "MAINTENANCE_ATTACHMENT_REMOVE_INVALID_STATE");
        }

        return MediatR.Unit.Value;
    }
}
