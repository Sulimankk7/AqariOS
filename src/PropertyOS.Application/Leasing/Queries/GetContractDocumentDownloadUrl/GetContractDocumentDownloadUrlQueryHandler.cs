using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Options;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Files;
using PropertyOS.Application.Files.Options;
using PropertyOS.Application.Files.Services;

namespace PropertyOS.Application.Leasing.Queries.GetContractDocumentDownloadUrl;

public class GetContractDocumentDownloadUrlQueryHandler : IRequestHandler<GetContractDocumentDownloadUrlQuery, ContractDocumentDownloadUrlDto>
{
    private readonly ITenantContext _tenantContext;
    private readonly ILeaseContractRepository _leaseContractRepository;
    private readonly IFileStorageRepository _fileStorageRepository;
    private readonly IStorageProvider _storageProvider;
    private readonly FileStorageOptions _options;

    public GetContractDocumentDownloadUrlQueryHandler(
        ITenantContext tenantContext,
        ILeaseContractRepository leaseContractRepository,
        IFileStorageRepository fileStorageRepository,
        IStorageProvider storageProvider,
        IOptions<FileStorageOptions> options)
    {
        _tenantContext = tenantContext;
        _leaseContractRepository = leaseContractRepository;
        _fileStorageRepository = fileStorageRepository;
        _storageProvider = storageProvider;
        _options = options.Value;
    }

    public async Task<ContractDocumentDownloadUrlDto> Handle(GetContractDocumentDownloadUrlQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");

        // 1. Fetch document and verify contract ownership & company boundary
        var document = await _leaseContractRepository.GetDocumentByIdAsync(request.LeaseContractId, request.DocumentId, companyId, cancellationToken);
        if (document == null)
        {
            throw new NotFoundException($"Contract document with ID '{request.DocumentId}' was not found.");
        }

        // 2. Fetch underlying file_storage record
        var fileStorage = await _fileStorageRepository.GetByIdAsync(document.FileId, cancellationToken);
        if (fileStorage == null)
        {
            throw new NotFoundException($"File storage record for document '{request.DocumentId}' was not found.");
        }

        // 3. Generate presigned URL with original filename and requested disposition (inline vs attachment)
        var url = await _storageProvider.GeneratePreSignedDownloadUrlAsync(
            fileStorage.StorageKey,
            fileStorage.OriginalFilename,
            _options.PreSignedUrlExpirationMinutes,
            request.Inline,
            cancellationToken);

        return new ContractDocumentDownloadUrlDto(url, fileStorage.OriginalFilename);
    }
}
