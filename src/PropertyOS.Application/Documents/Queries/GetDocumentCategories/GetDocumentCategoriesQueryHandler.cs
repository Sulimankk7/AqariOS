using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Documents.DTOs;

namespace PropertyOS.Application.Documents.Queries.GetDocumentCategories;

public class GetDocumentCategoriesQueryHandler : IRequestHandler<GetDocumentCategoriesQuery, List<DocumentCategoryDto>>
{
    private readonly ITenantContext _tenantContext;
    private readonly IDocumentCategoryRepository _categoryRepository;

    public GetDocumentCategoriesQueryHandler(
        ITenantContext tenantContext,
        IDocumentCategoryRepository categoryRepository)
    {
        _tenantContext = tenantContext;
        _categoryRepository = categoryRepository;
    }

    public async Task<List<DocumentCategoryDto>> Handle(GetDocumentCategoriesQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");

        var categories = await _categoryRepository.GetByCompanyIdAsync(companyId, cancellationToken);

        return categories
            .Select(c => new DocumentCategoryDto(
                Id: c.Id,
                CompanyId: c.CompanyId,
                Name: c.Name,
                Description: c.Description,
                CreatedAt: c.CreatedAt
            ))
            .ToList();
    }
}
