using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Marketplace.Commands.UpdateViewingRequestStatus;

public class UpdateViewingRequestStatusCommandHandler : IRequestHandler<UpdateViewingRequestStatusCommand, Unit>
{
    private readonly IViewingRequestRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public UpdateViewingRequestStatusCommandHandler(
        IViewingRequestRepository repository,
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<Unit> Handle(UpdateViewingRequestStatusCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId
            ?? throw new InvalidOperationException("Tenant context is required.");

        var viewingRequest = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (viewingRequest == null)
            throw new NotFoundException($"ViewingRequest with ID {request.Id} was not found.");

        if (viewingRequest.CompanyId != companyId)
            throw new NotFoundException($"ViewingRequest with ID {request.Id} was not found.");

        var now = DateTimeOffset.UtcNow;
        var userId = _currentUserContext.UserId;

        try
        {
            viewingRequest.UpdateStatus(request.Status, request.StaffNotes, now, userId);
        }
        catch (InvalidOperationException ex)
        {
            throw new BusinessRuleException(ex.Message);
        }

        return Unit.Value;
    }
}
