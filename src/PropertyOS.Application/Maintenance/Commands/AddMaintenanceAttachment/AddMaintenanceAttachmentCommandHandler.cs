using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Maintenance;

namespace PropertyOS.Application.Maintenance.Commands.AddMaintenanceAttachment;

public class AddMaintenanceAttachmentCommandHandler : IRequestHandler<AddMaintenanceAttachmentCommand, Guid>
{
    private readonly IMaintenanceRequestRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public AddMaintenanceAttachmentCommandHandler(
        IMaintenanceRequestRepository repository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Guid> Handle(AddMaintenanceAttachmentCommand request, CancellationToken cancellationToken)
    {
        _ = _tenantContext.CompanyId
            ?? throw new InvalidOperationException("Tenant context is required.");

        var maintenanceRequest = await _repository.GetByIdAsync(request.RequestId, cancellationToken)
            ?? throw new NotFoundException($"Maintenance request '{request.RequestId}' was not found.");

        // NOTE: Attachment file existence validation (via IMaintenanceRequestRepository.FileExistsAsync)
        // is intentionally deferred because the File storage module has not yet been implemented.
        // Once the File module exists, this check can be re-enabled here and in the corresponding integration/unit tests.

        var now = DateTimeOffset.UtcNow;
        var userId = _currentUserContext.UserId;

        // UploadedBy defaults to current user if not explicitly provided.
        // This supports future integrations (tenant portal, imports) where the
        // uploader and the current system actor are different identities.
        var uploadedBy = request.UploadedBy ?? userId;

        MaintenanceRequestAttachment attachment;
        try
        {
            attachment = maintenanceRequest.AddAttachment(
                fileId: request.FileId,
                uploadedBy: uploadedBy,
                description: request.Description,
                now: now,
                createdBy: userId);
        }
        catch (InvalidOperationException ex)
        {
            // Domain rules: duplicate active file on this request, or the request is deleted
            throw new BusinessRuleException(ex.Message, "MAINTENANCE_ATTACHMENT_ADD_INVALID_STATE");
        }

        // Explicit Add: the attachment carries a client-generated ID and its parent request is
        // already tracked, so navigation discovery alone would mark it Modified, not Added.
        await _repository.AddAttachmentAsync(attachment, cancellationToken);

        return attachment.Id;
    }
}
