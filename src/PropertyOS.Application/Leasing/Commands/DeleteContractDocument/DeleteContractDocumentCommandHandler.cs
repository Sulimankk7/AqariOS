using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Leasing.Commands.DeleteContractDocument;

public class DeleteContractDocumentCommandHandler : IRequestHandler<DeleteContractDocumentCommand, Unit>
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ILeaseContractRepository _leaseContractRepository;

    public DeleteContractDocumentCommandHandler(
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        ILeaseContractRepository leaseContractRepository)
    {
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
        _leaseContractRepository = leaseContractRepository;
    }

    public async Task<Unit> Handle(DeleteContractDocumentCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");
        var userId = _currentUserContext.UserId;

        // 1. Fetch document and verify contract ownership & company boundary
        var document = await _leaseContractRepository.GetDocumentByIdAsync(request.LeaseContractId, request.DocumentId, companyId, cancellationToken);
        if (document == null)
        {
            throw new NotFoundException($"Contract document with ID '{request.DocumentId}' was not found.");
        }

        // 2. Soft-delete document (sets DeletedAt; physical storage blob is NOT deleted synchronously)
        var now = DateTimeOffset.UtcNow;
        document.SoftDelete(now, userId);

        return Unit.Value;
    }
}
