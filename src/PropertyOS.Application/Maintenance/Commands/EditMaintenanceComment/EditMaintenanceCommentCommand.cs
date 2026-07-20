using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Maintenance.Commands.EditMaintenanceComment;

public record EditMaintenanceCommentCommand(
    Guid RequestId,
    Guid CommentId,
    string NewText
) : IRequest<MediatR.Unit>;

public class EditMaintenanceCommentCommandHandler
    : IRequestHandler<EditMaintenanceCommentCommand, MediatR.Unit>
{
    private readonly IMaintenanceRequestRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public EditMaintenanceCommentCommandHandler(
        IMaintenanceRequestRepository repository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<MediatR.Unit> Handle(
        EditMaintenanceCommentCommand request,
        CancellationToken cancellationToken)
    {
        _ = _tenantContext.CompanyId
            ?? throw new InvalidOperationException("Tenant context is required.");

        var maintenanceRequest = await _repository.GetByIdAsync(request.RequestId, cancellationToken)
            ?? throw new KeyNotFoundException($"Maintenance request '{request.RequestId}' was not found.");

        maintenanceRequest.EditComment(
            commentId: request.CommentId,
            newText: request.NewText,
            updatedAt: DateTimeOffset.UtcNow,
            updatedBy: _currentUserContext.UserId);

        return MediatR.Unit.Value;
    }
}
