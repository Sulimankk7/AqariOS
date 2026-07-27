using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Maintenance.Commands.RemoveMaintenanceComment;

public record RemoveMaintenanceCommentCommand(Guid RequestId, Guid CommentId) : ICommand;

public class RemoveMaintenanceCommentCommandHandler
    : IRequestHandler<RemoveMaintenanceCommentCommand, MediatR.Unit>
{
    private readonly IMaintenanceRequestRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public RemoveMaintenanceCommentCommandHandler(
        IMaintenanceRequestRepository repository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<MediatR.Unit> Handle(
        RemoveMaintenanceCommentCommand request,
        CancellationToken cancellationToken)
    {
        _ = _tenantContext.CompanyId
            ?? throw new InvalidOperationException("Tenant context is required.");

        var maintenanceRequest = await _repository.GetByIdAsync(request.RequestId, cancellationToken)
            ?? throw new NotFoundException($"Maintenance request '{request.RequestId}' was not found.");

        try
        {
            maintenanceRequest.RemoveComment(
                commentId: request.CommentId,
                now: DateTimeOffset.UtcNow,
                deletedBy: _currentUserContext.UserId);
        }
        catch (KeyNotFoundException ex)
        {
            // Domain lookup: the comment does not exist (or is already deleted) on this request
            throw new NotFoundException(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            // Domain rule: comments cannot be removed from a deleted maintenance request
            throw new BusinessRuleException(ex.Message, "MAINTENANCE_COMMENT_REMOVE_INVALID_STATE");
        }

        return MediatR.Unit.Value;
    }
}
