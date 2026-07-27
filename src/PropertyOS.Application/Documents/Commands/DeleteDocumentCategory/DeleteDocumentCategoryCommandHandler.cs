using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
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
            throw new NotFoundException($"Document category with ID '{request.Id}' was not found.");
        }

        var inUse = await _categoryRepository.IsCategoryInUseAsync(request.Id, companyId, cancellationToken);
        if (inUse)
        {
            throw new BusinessRuleException($"Cannot delete category '{category.Name}' because it is currently referenced by one or more building documents.", "DOCUMENT_CATEGORY_IN_USE");
        }

        var now = DateTimeOffset.UtcNow;
        try
        {
            category.SoftDelete(now, userId);
        }
        catch (InvalidOperationException ex)
        {
            throw new BusinessRuleException(ex.Message, "DOCUMENT_CATEGORY_INVALID_STATE");
        }

        return Unit.Value;
    }
}
