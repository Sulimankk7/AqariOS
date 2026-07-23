using System;
using FluentAssertions;
using PropertyOS.Domain.Documents.Entities;
using Xunit;

namespace PropertyOS.Tests.Unit.Domain.Documents;

public class BuildingDocumentDomainTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();
    private static readonly Guid BuildingId = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();
    private static readonly Guid FileId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidParameters_ShouldSucceed()
    {
        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.DateTime);

        var doc = BuildingDocument.Create(
            companyId: CompanyId,
            buildingId: BuildingId,
            categoryId: CategoryId,
            fileId: FileId,
            documentName: "Building License 2026",
            description: "Official municipality license",
            issueDate: today.AddDays(-10),
            expiryDate: today.AddDays(355),
            isConfidential: true,
            uploadedBy: Guid.NewGuid(),
            now: now,
            createdBy: Guid.NewGuid(),
            currentDate: today
        );

        doc.Should().NotBeNull();
        doc.CompanyId.Should().Be(CompanyId);
        doc.BuildingId.Should().Be(BuildingId);
        doc.CategoryId.Should().Be(CategoryId);
        doc.FileId.Should().Be(FileId);
        doc.DocumentName.Should().Be("Building License 2026");
        doc.IsConfidential.Should().BeTrue();
        doc.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void Create_WithFutureIssueDate_ShouldThrowArgumentException()
    {
        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.DateTime);

        Action act = () => BuildingDocument.Create(
            companyId: CompanyId,
            buildingId: BuildingId,
            categoryId: CategoryId,
            fileId: FileId,
            documentName: "Ownership Deed",
            description: null,
            issueDate: today.AddDays(5), // Future date
            expiryDate: null,
            isConfidential: false,
            uploadedBy: null,
            now: now,
            createdBy: null,
            currentDate: today
        );

        act.Should().Throw<ArgumentException>()
           .WithMessage("*cannot be in the future*");
    }

    [Fact]
    public void Create_WithExpiryBeforeOrEqualIssueDate_ShouldThrowArgumentException()
    {
        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.DateTime);

        Action act = () => BuildingDocument.Create(
            companyId: CompanyId,
            buildingId: BuildingId,
            categoryId: CategoryId,
            fileId: FileId,
            documentName: "Insurance Policy",
            description: null,
            issueDate: today.AddDays(-5),
            expiryDate: today.AddDays(-5), // Same day as issue
            isConfidential: false,
            uploadedBy: null,
            now: now,
            createdBy: null,
            currentDate: today
        );

        act.Should().Throw<ArgumentException>()
           .WithMessage("*must strictly succeed issue date*");
    }

    [Fact]
    public void SoftDelete_ShouldSetDeletedAtAndDeletedBy()
    {
        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.DateTime);
        var deleterId = Guid.NewGuid();

        var doc = BuildingDocument.Create(
            companyId: CompanyId,
            buildingId: BuildingId,
            categoryId: CategoryId,
            fileId: FileId,
            documentName: "Test Document",
            description: null,
            issueDate: null,
            expiryDate: null,
            isConfidential: false,
            uploadedBy: null,
            now: now,
            createdBy: null,
            currentDate: today
        );

        doc.SoftDelete(now, deleterId);

        doc.DeletedAt.Should().Be(now);
        doc.DeletedBy.Should().Be(deleterId);
    }
}
