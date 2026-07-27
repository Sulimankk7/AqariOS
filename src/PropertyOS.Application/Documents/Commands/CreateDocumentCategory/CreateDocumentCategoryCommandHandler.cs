using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Documents.DTOs;
using PropertyOS.Domain.Documents.Entities;

namespace PropertyOS.Application.Documents.Commands.CreateDocumentCategory;

public class CreateDocumentCategoryCommandHandler : IRequestHandler<CreateDocumentCategoryCommand, DocumentCategoryDto>
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDocumentCategoryRepository _categoryRepository;

    public CreateDocumentCategoryCommandHandler(
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        IDocumentCategoryRepository categoryRepository)
    {
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
        _categoryRepository = categoryRepository;
    }

    public async Task<DocumentCategoryDto> Handle(CreateDocumentCategoryCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");
        var userId = _currentUserContext.UserId;

        var nameExists = await _categoryRepository.ExistsByNameAsync(companyId, request.Name, null, cancellationToken);
        if (nameExists)
        {
            throw new ConflictException($"A document category named '{request.Name}' already exists within this company.");
        }

        var now = DateTimeOffset.UtcNow;
        var category = DocumentCategory.Create(
            companyId: companyId,
            name: request.Name,
            description: request.Description,
            now: now,
            createdBy: userId
        );

        await _categoryRepository.AddAsync(category, cancellationToken);

        return new DocumentCategoryDto(
            Id: category.Id,
            CompanyId: category.CompanyId,
            Name: category.Name,
            Description: category.Description,
            CreatedAt: category.CreatedAt
        );
    }
}
