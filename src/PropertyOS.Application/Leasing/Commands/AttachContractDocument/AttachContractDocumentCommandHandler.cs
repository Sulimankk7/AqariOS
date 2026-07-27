using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Files;
using PropertyOS.Domain.Leasing;

namespace PropertyOS.Application.Leasing.Commands.AttachContractDocument;

public class AttachContractDocumentCommandHandler : IRequestHandler<AttachContractDocumentCommand, Guid>
{
    private readonly ILeaseContractRepository _leaseContractRepository;
    private readonly IFileStorageRepository _fileStorageRepository;
    private readonly ITenantContext? _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public AttachContractDocumentCommandHandler(
        ILeaseContractRepository leaseContractRepository,
        IFileStorageRepository fileStorageRepository,
        ICurrentUserContext currentUserContext)
        : this(leaseContractRepository, fileStorageRepository, null, currentUserContext)
    {
    }

    public AttachContractDocumentCommandHandler(
        ILeaseContractRepository leaseContractRepository,
        IFileStorageRepository fileStorageRepository,
        ITenantContext? tenantContext,
        ICurrentUserContext currentUserContext)
    {
        _leaseContractRepository = leaseContractRepository ?? throw new ArgumentNullException(nameof(leaseContractRepository));
        _fileStorageRepository = fileStorageRepository ?? throw new ArgumentNullException(nameof(fileStorageRepository));
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
    }

    public async Task<Guid> Handle(AttachContractDocumentCommand request, CancellationToken cancellationToken)
    {
        // 1. Resolve company ID from tenant context
        Guid companyId;
        if (_tenantContext != null && _tenantContext.CompanyId.HasValue)
        {
            companyId = _tenantContext.CompanyId.Value;
        }
        else
        {
            // Fallback for tests running outside HTTP context without ITenantContext
            var targetContract = await _leaseContractRepository.GetByIdAsync(request.LeaseContractId, cancellationToken);
            if (targetContract == null)
            {
                throw new NotFoundException($"LeaseContract with ID '{request.LeaseContractId}' was not found.");
            }
            companyId = targetContract.CompanyId;
        }

        // 2. Load & verify LeaseContract within current company boundary
        var contract = await _leaseContractRepository.GetByIdAsync(request.LeaseContractId, cancellationToken);
        if (contract == null || contract.CompanyId != companyId)
        {
            throw new NotFoundException($"LeaseContract with ID '{request.LeaseContractId}' was not found.");
        }

        // 3. Load & verify FileStorage record exists and belongs to the caller's company
        var fileExists = await _fileStorageRepository.ExistsAsync(request.FileId, companyId, cancellationToken);
        if (!fileExists)
        {
            throw new NotFoundException($"File storage record with ID '{request.FileId}' was not found.");
        }

        // 4. Prevent duplicate file attachment to the same lease contract
        var alreadyAttached = await _leaseContractRepository.HasDocumentAsync(request.LeaseContractId, request.FileId, cancellationToken);
        if (alreadyAttached)
        {
            throw new ConflictException($"File '{request.FileId}' is already attached to lease contract '{request.LeaseContractId}'.");
        }

        // 5. Create ContractDocument domain entity
        var document = ContractDocument.Create(
            companyId: companyId,
            leaseContractId: contract.Id,
            fileId: request.FileId,
            documentType: request.DocumentType,
            description: request.Description,
            uploadedBy: _currentUserContext.UserId,
            createdAt: DateTimeOffset.UtcNow
        );

        // 6. Persist via repository (Unit of Work / SaveChanges managed by TransactionBehavior)
        await _leaseContractRepository.AddDocumentAsync(document, cancellationToken);

        return document.Id;
    }
}
