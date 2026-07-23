using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Documents.Commands.DeleteBuildingDocument;

public class DeleteBuildingDocumentCommandHandler : IRequestHandler<DeleteBuildingDocumentCommand, Unit>
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IBuildingDocumentRepository _documentRepository;

    public DeleteBuildingDocumentCommandHandler(
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        IBuildingDocumentRepository documentRepository)
    {
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
        _documentRepository = documentRepository;
    }

    public async Task<Unit> Handle(DeleteBuildingDocumentCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");
        var userId = _currentUserContext.UserId;

        var document = await _documentRepository.GetByIdAsync(request.Id, companyId, cancellationToken);
        if (document == null)
        {
            throw new KeyNotFoundException($"Building document with ID '{request.Id}' was not found.");
        }

        var now = DateTimeOffset.UtcNow;
        document.SoftDelete(now, userId);

        return Unit.Value;
    }
}
