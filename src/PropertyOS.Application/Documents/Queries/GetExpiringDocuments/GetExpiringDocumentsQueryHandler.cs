using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Documents.DTOs;

namespace PropertyOS.Application.Documents.Queries.GetExpiringDocuments;

public class GetExpiringDocumentsQueryHandler : IRequestHandler<GetExpiringDocumentsQuery, List<ExpiringDocumentDto>>
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IBuildingDocumentRepository _documentRepository;

    public GetExpiringDocumentsQueryHandler(
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        IBuildingDocumentRepository documentRepository)
    {
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
        _documentRepository = documentRepository;
    }

    public async Task<List<ExpiringDocumentDto>> Handle(GetExpiringDocumentsQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");
        var userId = _currentUserContext.UserId;

        var canViewConfidential = userId.HasValue && await _documentRepository.CanUserViewConfidentialDocumentsAsync(userId.Value, companyId, cancellationToken);

        return await _documentRepository.GetExpiringDocumentsAsync(
            companyId: companyId,
            withinDays: request.WithinDays,
            canViewConfidential: canViewConfidential,
            cancellationToken: cancellationToken
        );
    }
}
