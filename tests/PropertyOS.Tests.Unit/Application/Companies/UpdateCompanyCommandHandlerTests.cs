using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Companies;
using PropertyOS.Application.Companies.Commands.UpdateCompany;
using PropertyOS.Domain.Companies;
using PropertyOS.Domain.Companies.Enums;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Companies;

public class UpdateCompanyCommandHandlerTests
{
    private readonly ICompanyRepository _companyRepository = Substitute.For<ICompanyRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly ICurrentUserContext _currentUserContext = Substitute.For<ICurrentUserContext>();
    private readonly UpdateCompanyCommandHandler _handler;

    private static readonly Guid CompanyId = Guid.NewGuid();

    public UpdateCompanyCommandHandlerTests()
    {
        _tenantContext.CompanyId.Returns(CompanyId);
        _tenantContext.IsPlatformAdmin.Returns(false);
        _currentUserContext.UserId.Returns((Guid?)Guid.NewGuid());
        _handler = new UpdateCompanyCommandHandler(_companyRepository, _tenantContext, _currentUserContext);
    }

    private static Company CreateCompany()
    {
        return Company.Create(
            "Old Legal Name",
            "Old Display Name",
            "+962790000000",
            CompanyType.IndividualOwner,
            "JO",
            DateTimeOffset.UtcNow,
            null);
    }

    [Fact]
    public async Task Handle_WithOwnCompany_ShouldUpdateProfile()
    {
        // Arrange
        var company = CreateCompany();
        _companyRepository.GetByIdAsync(CompanyId, Arg.Any<CancellationToken>()).Returns(company);

        var command = new UpdateCompanyCommand(
            CompanyId,
            "New Legal Name",
            "New Display Name",
            "+962791111111",
            "new@aqari.jo");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        company.LegalName.Should().Be("New Legal Name");
        company.DisplayName.Should().Be("New Display Name");
        company.PrimaryPhone.Should().Be("+962791111111");
        company.PrimaryEmail.Should().Be("new@aqari.jo");
    }

    [Fact]
    public async Task Handle_ForDifferentCompany_ShouldThrowNotFoundExceptionWithoutTouchingRepository()
    {
        // Arrange — cross-tenant update must be masked as 404, never as 403.
        var command = new UpdateCompanyCommand(
            Guid.NewGuid(),
            "Legal",
            "Display",
            "+962790000000",
            null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
        await _companyRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithoutTenantContext_ShouldThrowNotFoundException()
    {
        // Arrange — null CompanyId is fail-closed.
        _tenantContext.CompanyId.Returns((Guid?)null);
        var command = new UpdateCompanyCommand(CompanyId, "Legal", "Display", "+962790000000", null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenCompanyNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        _companyRepository.GetByIdAsync(CompanyId, Arg.Any<CancellationToken>()).Returns((Company?)null);
        var command = new UpdateCompanyCommand(CompanyId, "Legal", "Display", "+962790000000", null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(command, CancellationToken.None));
    }
}
