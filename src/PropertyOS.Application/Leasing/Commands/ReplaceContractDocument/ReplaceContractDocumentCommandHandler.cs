using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Files;

namespace PropertyOS.Application.Leasing.Commands.ReplaceContractDocument;

public class ReplaceContractDocumentCommandHandler : IRequestHandler<ReplaceContractDocumentCommand, Unit>
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ILeaseContractRepository _leaseContractRepository;
    private readonly IFileStorageRepository _fileStorageRepository;

    public ReplaceContractDocumentCommandHandler(
        ITenantContext tenantContext,
        ICurrentUserContext currentUserContext,
        ILeaseContractRepository leaseContractRepository,
        IFileStorageRepository fileStorageRepository)
    {
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
        _leaseContractRepository = leaseContractRepository;
        _fileStorageRepository = fileStorageRepository;
    }

    public async Task<Unit> Handle(ReplaceContractDocumentCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");
        var userId = _currentUserContext.UserId;

        // 1. Fetch document and verify contract ownership & company boundary
        var document = await _leaseContractRepository.GetDocumentByIdAsync(request.LeaseContractId, request.DocumentId, companyId, cancellationToken);
        if (document == null)
        {
            throw new NotFoundException($"Contract document with ID '{request.DocumentId}' was not found.");
        }

        // 2. Verify new file_storage record exists
        var newFileExists = await _fileStorageRepository.ExistsAsync(request.NewFileId, companyId, cancellationToken);
        if (!newFileExists)
        {
            throw new NotFoundException($"File storage record with ID '{request.NewFileId}' was not found.");
        }

        // 3. Update document to reference the new file_id (leaving the previous file_storage record untouched)
        var now = DateTimeOffset.UtcNow;
        document.ReplaceFile(request.NewFileId, request.Description, userId, now);

        return Unit.Value;
    }
}
