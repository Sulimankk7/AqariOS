using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Files;
using PropertyOS.Application.Files.Commands.ConfirmFileUpload;
using PropertyOS.Application.Files.Services;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Files;

/// <summary>
/// Unit tests for ConfirmFileUploadCommandHandler, focused on the storage-key
/// canonicalization gate: non-canonical keys (traversal segments, backslashes)
/// must be rejected BEFORE the tenant-prefix and FileId checks, because those
/// checks are purely textual and a non-canonical key could satisfy them while
/// resolving outside the tenant's directory on the storage backend.
/// </summary>
public class ConfirmFileUploadCommandHandlerTests
{
    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _fileId = Guid.NewGuid();

    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserContext _currentUserContext = Substitute.For<ICurrentUserContext>();
    private readonly IFileStorageRepository _fileRepository = Substitute.For<IFileStorageRepository>();
    private readonly IStorageProvider _storageProvider = Substitute.For<IStorageProvider>();
    private readonly IFileValidationService _validationService = Substitute.For<IFileValidationService>();

    public ConfirmFileUploadCommandHandlerTests()
    {
        _tenantContext.CompanyId.Returns(_companyId);
        _currentUserContext.UserId.Returns((Guid?)Guid.NewGuid());
    }

    private ConfirmFileUploadCommandHandler CreateHandler()
        => new(_tenantContext, _currentUserContext, _fileRepository, _storageProvider, _validationService);

    private ConfirmFileUploadCommand CreateCommand(string storageKey)
        => new(
            FileId: _fileId,
            StorageKey: storageKey,
            OriginalFilename: "deed.pdf",
            MimeType: "application/pdf",
            SizeBytes: 1024);

    private async Task AssertRejectedAsKeyInvalid(string storageKey)
    {
        var handler = CreateHandler();

        Func<Task> act = () => handler.Handle(CreateCommand(storageKey), CancellationToken.None);

        (await act.Should().ThrowAsync<BusinessRuleException>())
            .Which.Code.Should().Be("FILE_STORAGE_KEY_INVALID");

        // The key must be rejected before any storage backend interaction.
        await _storageProvider.DidNotReceiveWithAnyArgs().ExistsAsync(default!, default);
        await _fileRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_StorageKeyWithTraversalSegment_ThrowsFileStorageKeyInvalid()
    {
        // Starts with the correct tenant prefix and embeds the FileId, but the ".."
        // segment could escape the tenant directory on the storage backend.
        var key = $"{_companyId:D}/../{Guid.NewGuid():D}/{_fileId:D}-deed.pdf";
        await AssertRejectedAsKeyInvalid(key);
    }

    [Fact]
    public async Task Handle_StorageKeyWithBackslash_ThrowsFileStorageKeyInvalid()
    {
        var key = $"{_companyId:D}\\documents\\{_fileId:D}-deed.pdf";
        await AssertRejectedAsKeyInvalid(key);
    }

    [Fact]
    public async Task Handle_EmptyStorageKey_ThrowsFileStorageKeyInvalid()
    {
        await AssertRejectedAsKeyInvalid(string.Empty);
    }

    [Fact]
    public async Task Handle_StorageKeyOutsideTenantPrefix_ThrowsFileStorageKeyInvalid()
    {
        var key = $"{Guid.NewGuid():D}/documents/{_fileId:D}-deed.pdf";
        await AssertRejectedAsKeyInvalid(key);
    }

    [Fact]
    public async Task Handle_StorageKeyMissingRequestedFileId_ThrowsFileStorageKeyInvalid()
    {
        var key = $"{_companyId:D}/documents/{Guid.NewGuid():D}-deed.pdf";
        await AssertRejectedAsKeyInvalid(key);
    }

    [Fact]
    public async Task Handle_CanonicalKeyUnderTenantPrefix_ConfirmsUpload()
    {
        var key = $"{_companyId:D}/documents/{_fileId:D}-deed.pdf";
        const long sizeBytes = 1024;

        _storageProvider.ExistsAsync(key, Arg.Any<CancellationToken>()).Returns(true);
        _storageProvider.GetSizeAsync(key, Arg.Any<CancellationToken>()).Returns(sizeBytes);
        _storageProvider.GetReadStreamAsync(key, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Stream>(new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 })));
        _validationService.SanitizeFilename("deed.pdf").Returns("deed.pdf");

        var handler = CreateHandler();

        var result = await handler.Handle(CreateCommand(key), CancellationToken.None);

        result.Id.Should().Be(_fileId);
        result.CompanyId.Should().Be(_companyId);
        result.StorageKey.Should().Be(key);
        await _fileRepository.ReceivedWithAnyArgs(1).AddAsync(default!, default);
    }
}
