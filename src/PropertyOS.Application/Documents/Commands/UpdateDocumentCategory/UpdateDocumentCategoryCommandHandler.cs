using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Documents.DTOs;

namespace PropertyOS.Application.Documents.Commands.UpdateDocumentCategory;

public class UpdateDocumentCategoryCommandHandler : IRequestHandler<UpdateDocumentCategoryCommand, DocumentCategoryDto>
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDocumentCategoryRepository _categoryRepository;

    public UpdateDocumentCategoryCommandHandler(
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        IDocumentCategoryRepository categoryRepository)
    {
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
        _categoryRepository = categoryRepository;
    }

    public async Task<DocumentCategoryDto> Handle(UpdateDocumentCategoryCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");
        var userId = _currentUserContext.UserId;

        var category = await _categoryRepository.GetByIdAsync(request.Id, companyId, cancellationToken);
        if (category == null)
        {
            throw new KeyNotFoundException($"Document category with ID '{request.Id}' was not found.");
        }

        var nameExists = await _categoryRepository.ExistsByNameAsync(companyId, request.Name, request.Id, cancellationToken);
        if (nameExists)
        {
            throw new InvalidOperationException($"Another document category named '{request.Name}' already exists within this company.");
        }

        var now = DateTimeOffset.UtcNow;
        category.UpdateDetails(request.Name, request.Description, now, userId);

        return new DocumentCategoryDto(
            Id: category.Id,
            CompanyId: category.CompanyId,
            Name: category.Name,
            Description: category.Description,
            CreatedAt: category.CreatedAt
        );
    }
}
