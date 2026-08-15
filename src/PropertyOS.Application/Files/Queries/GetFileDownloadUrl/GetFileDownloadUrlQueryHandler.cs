using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Options;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Files.DTOs;
using PropertyOS.Application.Files.Options;
using PropertyOS.Application.Files.Services;

namespace PropertyOS.Application.Files.Queries.GetFileDownloadUrl;

public class GetFileDownloadUrlQueryHandler : IRequestHandler<GetFileDownloadUrlQuery, FileDownloadUrlResponse>
{
    private readonly ITenantContext _tenantContext;
    private readonly IFileStorageRepository _fileRepository;
    private readonly IStorageProvider _storageProvider;
    private readonly FileStorageOptions _options;

    public GetFileDownloadUrlQueryHandler(
        ITenantContext tenantContext,
        IFileStorageRepository fileRepository,
        IStorageProvider storageProvider,
        IOptions<FileStorageOptions> options)
    {
        _tenantContext = tenantContext;
        _fileRepository = fileRepository;
        _storageProvider = storageProvider;
        _options = options.Value;
    }

    public async Task<FileDownloadUrlResponse> Handle(GetFileDownloadUrlQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");

        var fileStorage = await _fileRepository.GetByIdAsync(request.FileId, cancellationToken);
        if (fileStorage == null || fileStorage.CompanyId != companyId)
        {
            throw new NotFoundException($"File with ID '{request.FileId}' was not found.");
        }

        var downloadUrl = await _storageProvider.GeneratePreSignedDownloadUrlAsync(
            storageKey: fileStorage.StorageKey,
            filename: fileStorage.OriginalFilename,
            expirationMinutes: _options.PreSignedUrlExpirationMinutes,
            inline: request.Inline,
            cancellationToken: cancellationToken
        );

        return new FileDownloadUrlResponse(
            FileId: fileStorage.Id,
            DownloadUrl: downloadUrl,
            ExpirationMinutes: _options.PreSignedUrlExpirationMinutes,
            MimeType: fileStorage.MimeType,
            OriginalFilename: fileStorage.OriginalFilename
        );
    }
}
