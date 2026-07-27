using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Documents.DTOs;

namespace PropertyOS.Application.Documents.Queries.GetBuildingDocumentById;

public class GetBuildingDocumentByIdQueryHandler : IRequestHandler<GetBuildingDocumentByIdQuery, BuildingDocumentDto>
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IBuildingDocumentRepository _documentRepository;

    public GetBuildingDocumentByIdQueryHandler(
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        IBuildingDocumentRepository documentRepository)
    {
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
        _documentRepository = documentRepository;
    }

    public async Task<BuildingDocumentDto> Handle(GetBuildingDocumentByIdQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");
        var userId = _currentUserContext.UserId;

        var canViewConfidential = userId.HasValue && await _documentRepository.CanUserViewConfidentialDocumentsAsync(userId.Value, companyId, cancellationToken);

        var document = await _documentRepository.GetBuildingDocumentDtoByIdAsync(
            id: request.Id,
            companyId: companyId,
            canViewConfidential: canViewConfidential,
            cancellationToken: cancellationToken
        );

        if (document == null)
        {
            throw new NotFoundException($"Building document with ID '{request.Id}' was not found.");
        }

        return document;
    }
}
