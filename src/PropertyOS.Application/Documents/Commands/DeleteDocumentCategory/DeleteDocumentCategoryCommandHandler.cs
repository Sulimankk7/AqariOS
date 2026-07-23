using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Documents.Commands.DeleteDocumentCategory;

public class DeleteDocumentCategoryCommandHandler : IRequestHandler<DeleteDocumentCategoryCommand, Unit>
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDocumentCategoryRepository _categoryRepository;

    public DeleteDocumentCategoryCommandHandler(
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        IDocumentCategoryRepository categoryRepository)
    {
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
        _categoryRepository = categoryRepository;
    }

    public async Task<Unit> Handle(DeleteDocumentCategoryCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");
        var userId = _currentUserContext.UserId;

        var category = await _categoryRepository.GetByIdAsync(request.Id, companyId, cancellationToken);
        if (category == null)
        {
            throw new KeyNotFoundException($"Document category with ID '{request.Id}' was not found.");
        }

        var inUse = await _categoryRepository.IsCategoryInUseAsync(request.Id, companyId, cancellationToken);
        if (inUse)
        {
            throw new InvalidOperationException($"Cannot delete category '{category.Name}' because it is currently referenced by one or more building documents.");
        }

        var now = DateTimeOffset.UtcNow;
        category.SoftDelete(now, userId);

        return Unit.Value;
    }
}
