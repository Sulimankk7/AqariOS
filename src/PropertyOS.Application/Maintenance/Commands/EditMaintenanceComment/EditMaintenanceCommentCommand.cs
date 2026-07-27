using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Maintenance.Commands.EditMaintenanceComment;

public record EditMaintenanceCommentCommand(
    Guid RequestId,
    Guid CommentId,
    string NewText
) : ICommand;

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
            ?? throw new NotFoundException($"Maintenance request '{request.RequestId}' was not found.");

        try
        {
            maintenanceRequest.EditComment(
                commentId: request.CommentId,
                newText: request.NewText,
                updatedAt: DateTimeOffset.UtcNow,
                updatedBy: _currentUserContext.UserId);
        }
        catch (KeyNotFoundException ex)
        {
            // Domain lookup: the comment does not exist (or is already deleted) on this request
            throw new NotFoundException(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            // Domain rule: comments cannot be edited on a deleted maintenance request
            throw new BusinessRuleException(ex.Message, "MAINTENANCE_COMMENT_EDIT_INVALID_STATE");
        }

        return MediatR.Unit.Value;
    }
}
