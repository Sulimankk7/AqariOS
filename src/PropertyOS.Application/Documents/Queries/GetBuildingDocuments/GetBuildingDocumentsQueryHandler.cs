using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Documents.DTOs;

namespace PropertyOS.Application.Documents.Queries.GetBuildingDocuments;

public class GetBuildingDocumentsQueryHandler : IRequestHandler<GetBuildingDocumentsQuery, PagedBuildingDocumentsResponse>
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IBuildingDocumentRepository _documentRepository;

    public GetBuildingDocumentsQueryHandler(
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        IBuildingDocumentRepository documentRepository)
    {
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
        _documentRepository = documentRepository;
    }

    public async Task<PagedBuildingDocumentsResponse> Handle(GetBuildingDocumentsQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");
        var userId = _currentUserContext.UserId;

        var canViewConfidential = userId.HasValue && await _documentRepository.CanUserViewConfidentialDocumentsAsync(userId.Value, companyId, cancellationToken);

        return await _documentRepository.GetBuildingDocumentsAsync(
            companyId: companyId,
            buildingId: request.BuildingId,
            categoryId: request.CategoryId,
            searchTerm: request.SearchTerm,
            canViewConfidential: canViewConfidential,
            pageNumber: request.PageNumber,
            pageSize: request.PageSize,
            cancellationToken: cancellationToken
        );
    }
}
