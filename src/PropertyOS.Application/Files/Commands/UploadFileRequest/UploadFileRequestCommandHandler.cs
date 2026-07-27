using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Files.Options;
using PropertyOS.Application.Files.Services;

namespace PropertyOS.Application.Files.Commands.UploadFileRequest;

public class UploadFileRequestCommandHandler : IRequestHandler<UploadFileRequestCommand, UploadFileRequestResponse>
{
    private readonly ITenantContext _tenantContext;
    private readonly IFileValidationService _validationService;
    private readonly IStorageProvider _storageProvider;
    private readonly FileStorageOptions _options;

    public UploadFileRequestCommandHandler(
        ITenantContext tenantContext,
        IFileValidationService validationService,
        IStorageProvider storageProvider,
        FileStorageOptions options)
    {
        _tenantContext = tenantContext;
        _validationService = validationService;
        _storageProvider = storageProvider;
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<UploadFileRequestResponse> Handle(UploadFileRequestCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantContext.CompanyId ?? throw new InvalidOperationException("Tenant context is required.");

        try
        {
            _validationService.ValidateFile(request.Filename, request.MimeType, request.SizeBytes, null!);
        }
        catch (ArgumentException ex)
        {
            throw new BusinessRuleException(ex.Message, "FILE_VALIDATION_FAILED");
        }

        var sanitized = _validationService.SanitizeFilename(request.Filename);
        var fileId = Guid.CreateVersion7();
        var storageKey = _validationService.GenerateStorageKey(companyId, request.ModuleName, request.EntityId, fileId, sanitized);

        var uploadUrl = await _storageProvider.GeneratePreSignedUploadUrlAsync(
            storageKey,
            _options.PreSignedUrlExpirationMinutes,
            cancellationToken);

        return new UploadFileRequestResponse(
            FileId: fileId,
            StorageKey: storageKey,
            UploadUrl: uploadUrl,
            ExpirationMinutes: _options.PreSignedUrlExpirationMinutes
        );
    }
}
