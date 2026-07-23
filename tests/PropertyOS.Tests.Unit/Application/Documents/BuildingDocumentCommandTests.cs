using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Documents;
using PropertyOS.Application.Documents.Commands.CreateBuildingDocument;
using PropertyOS.Application.Documents.Commands.ReplaceBuildingDocument;
using PropertyOS.Application.Files;
using PropertyOS.Domain.Documents.Entities;
using PropertyOS.Domain.Files.Entities;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Documents;

public class BuildingDocumentCommandTests
{
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserContext _currentUserContext = Substitute.For<ICurrentUserContext>();
    private readonly IBuildingDocumentRepository _documentRepository = Substitute.For<IBuildingDocumentRepository>();
    private readonly IDocumentCategoryRepository _categoryRepository = Substitute.For<IDocumentCategoryRepository>();
    private readonly IFileStorageRepository _fileRepository = Substitute.For<IFileStorageRepository>();

    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid BuildingId = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();
    private static readonly Guid FileId = Guid.NewGuid();

    public BuildingDocumentCommandTests()
    {
        _tenantContext.CompanyId.Returns(CompanyId);
        _currentUserContext.UserId.Returns(UserId);
    }

    [Fact]
    public async Task CreateBuildingDocument_WithValidInputs_ShouldReturnDto()
    {
        var category = DocumentCategory.Create(CompanyId, "Insurance", null, DateTimeOffset.UtcNow, UserId);
        var fileStorage = FileStorage.Create(CompanyId, UserId, "policy.pdf", "application/pdf", 2048, "key", DateTimeOffset.UtcNow, UserId);

        _categoryRepository.GetByIdAsync(CategoryId, CompanyId, Arg.Any<CancellationToken>()).Returns(category);
        _fileRepository.ExistsAsync(FileId, CompanyId, Arg.Any<CancellationToken>()).Returns(true);
        _fileRepository.GetByIdAsync(FileId, Arg.Any<CancellationToken>()).Returns(fileStorage);

        var handler = new CreateBuildingDocumentCommandHandler(
            _tenantContext,
            _currentUserContext,
            _documentRepository,
            _categoryRepository,
            _fileRepository);

        var command = new CreateBuildingDocumentCommand(
            BuildingId: BuildingId,
            CategoryId: CategoryId,
            FileId: FileId,
            DocumentName: "Building Policy 2026",
            Description: null,
            IssueDate: null,
            ExpiryDate: null,
            IsConfidential: false);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.DocumentName.Should().Be("Building Policy 2026");
        result.BuildingId.Should().Be(BuildingId);
        await _documentRepository.Received(1).AddAsync(Arg.Any<BuildingDocument>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplaceBuildingDocument_ShouldAtomicallySoftDeleteOldAndCreateNew()
    {
        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.DateTime);

        var oldDoc = BuildingDocument.Create(
            CompanyId, BuildingId, CategoryId, FileId, "Old Name", null, null, null, false, UserId, now, UserId, today);

        var newFileId = Guid.NewGuid();
        var category = DocumentCategory.Create(CompanyId, "Insurance", null, now, UserId);
        var newFileStorage = FileStorage.Create(CompanyId, UserId, "new_policy.pdf", "application/pdf", 4096, "new_key", now, UserId);

        _documentRepository.GetByIdAsync(oldDoc.Id, CompanyId, Arg.Any<CancellationToken>()).Returns(oldDoc);
        _fileRepository.ExistsAsync(newFileId, CompanyId, Arg.Any<CancellationToken>()).Returns(true);
        _categoryRepository.GetByIdAsync(CategoryId, CompanyId, Arg.Any<CancellationToken>()).Returns(category);
        _fileRepository.GetByIdAsync(newFileId, Arg.Any<CancellationToken>()).Returns(newFileStorage);

        var handler = new ReplaceBuildingDocumentCommandHandler(
            _tenantContext,
            _currentUserContext,
            _documentRepository,
            _categoryRepository,
            _fileRepository);

        var command = new ReplaceBuildingDocumentCommand(
            ExistingDocumentId: oldDoc.Id,
            NewFileId: newFileId,
            NewDocumentName: "Updated Policy 2026",
            NewDescription: null,
            NewIssueDate: null,
            NewExpiryDate: null,
            NewIsConfidential: null);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.DocumentName.Should().Be("Updated Policy 2026");
        oldDoc.DeletedAt.Should().NotBeNull(); // Old document soft-deleted
        await _documentRepository.Received(1).AddAsync(Arg.Any<BuildingDocument>(), Arg.Any<CancellationToken>());
    }
}
