using System;
using FluentAssertions;
using PropertyOS.Domain.Documents.Entities;
using Xunit;

namespace PropertyOS.Tests.Unit.Domain.Documents;

public class DocumentCategoryDomainTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidName_ShouldSucceed()
    {
        var now = DateTimeOffset.UtcNow;
        var category = DocumentCategory.Create(
            companyId: CompanyId,
            name: "Civil Defense Certificates",
            description: "Safety compliance filings",
            now: now,
            createdBy: Guid.NewGuid()
        );

        category.Should().NotBeNull();
        category.Name.Should().Be("Civil Defense Certificates");
        category.CompanyId.Should().Be(CompanyId);
        category.DeletedAt.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankName_ShouldThrowArgumentException(string invalidName)
    {
        var now = DateTimeOffset.UtcNow;

        Action act = () => DocumentCategory.Create(
            companyId: CompanyId,
            name: invalidName,
            description: null,
            now: now,
            createdBy: null
        );

        act.Should().Throw<ArgumentException>()
           .WithMessage("*cannot be blank*");
    }
}
