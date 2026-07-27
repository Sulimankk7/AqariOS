using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Domain.Maintenance;

namespace PropertyOS.Application.Maintenance.Commands.AddMaintenanceComment;

public record AddMaintenanceCommentCommand(
    Guid RequestId,
    string CommentText
) : ICommand<Guid>;

public class AddMaintenanceCommentCommandHandler : IRequestHandler<AddMaintenanceCommentCommand, Guid>
{
    private readonly IMaintenanceRequestRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public AddMaintenanceCommentCommandHandler(
        IMaintenanceRequestRepository repository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Guid> Handle(AddMaintenanceCommentCommand request, CancellationToken cancellationToken)
    {
        _ = _tenantContext.CompanyId
            ?? throw new InvalidOperationException("Tenant context is required.");

        var maintenanceRequest = await _repository.GetByIdAsync(request.RequestId, cancellationToken)
            ?? throw new NotFoundException($"Maintenance request '{request.RequestId}' was not found.");

        MaintenanceRequestComment comment;
        try
        {
            comment = maintenanceRequest.AddComment(
                commentText: request.CommentText,
                now: DateTimeOffset.UtcNow,
                createdBy: _currentUserContext.UserId);
        }
        catch (InvalidOperationException ex)
        {
            // Domain rule: comments cannot be added to a deleted maintenance request
            throw new BusinessRuleException(ex.Message, "MAINTENANCE_COMMENT_ADD_INVALID_STATE");
        }

        // Explicit Add: the comment carries a client-generated ID and its parent request is
        // already tracked, so navigation discovery alone would mark it Modified, not Added.
        await _repository.AddCommentAsync(comment, cancellationToken);

        return comment.Id;
    }
}
