using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Companies;
using PropertyOS.Application.Companies.Queries.Common;
using PropertyOS.Application.Companies.Queries.GetCompanyById;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Companies;

public class GetCompanyByIdQueryHandlerTests
{
    private readonly ICompanyRepository _companyRepository = Substitute.For<ICompanyRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly GetCompanyByIdQueryHandler _handler;

    private static readonly Guid CompanyId = Guid.NewGuid();

    public GetCompanyByIdQueryHandlerTests()
    {
        _tenantContext.CompanyId.Returns(CompanyId);
        _tenantContext.IsPlatformAdmin.Returns(false);
        _handler = new GetCompanyByIdQueryHandler(_companyRepository, _tenantContext);
    }

    [Fact]
    public async Task Handle_WithOwnCompany_ShouldReturnCompanyDetailDto()
    {
        // Arrange
        var dto = new CompanyDetailDto { Id = CompanyId, LegalName = "Aqari Co", DisplayName = "Aqari" };
        _companyRepository.GetDetailByIdAsync(CompanyId, Arg.Any<CancellationToken>()).Returns(dto);

        var query = new GetCompanyByIdQuery(CompanyId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(CompanyId);
        result.LegalName.Should().Be("Aqari Co");
    }

    [Fact]
    public async Task Handle_ForDifferentCompany_ShouldThrowNotFoundExceptionWithoutTouchingRepository()
    {
        // Arrange — cross-tenant request must be masked as 404, never as 403.
        var otherCompanyId = Guid.NewGuid();
        var query = new GetCompanyByIdQuery(otherCompanyId);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(query, CancellationToken.None));
        await _companyRepository.DidNotReceive().GetDetailByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithoutTenantContext_ShouldThrowNotFoundException()
    {
        // Arrange — null CompanyId is fail-closed (denied), never "all tenants".
        _tenantContext.CompanyId.Returns((Guid?)null);
        var query = new GetCompanyByIdQuery(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AsPlatformAdmin_ShouldAllowCrossTenantRead()
    {
        // Arrange
        _tenantContext.IsPlatformAdmin.Returns(true);
        var otherCompanyId = Guid.NewGuid();
        var dto = new CompanyDetailDto { Id = otherCompanyId, LegalName = "Other Co", DisplayName = "Other" };
        _companyRepository.GetDetailByIdAsync(otherCompanyId, Arg.Any<CancellationToken>()).Returns(dto);

        var query = new GetCompanyByIdQuery(otherCompanyId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(otherCompanyId);
    }

    [Fact]
    public async Task Handle_WhenCompanyNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        _companyRepository.GetDetailByIdAsync(CompanyId, Arg.Any<CancellationToken>())
            .Returns((CompanyDetailDto?)null);

        var query = new GetCompanyByIdQuery(CompanyId);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(query, CancellationToken.None));
    }
}
