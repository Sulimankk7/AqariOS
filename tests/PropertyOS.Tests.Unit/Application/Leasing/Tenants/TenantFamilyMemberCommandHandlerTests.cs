using System;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Leasing.Commands.CreateTenantFamilyMember;
using PropertyOS.Application.Leasing.Commands.DeleteTenantFamilyMember;
using PropertyOS.Application.Leasing.Commands.UpdateTenantFamilyMember;
using PropertyOS.Application.Leasing.Queries.GetFamilyMemberById;
using PropertyOS.Application.Leasing.Queries.GetFamilyMembersForTenant;
using PropertyOS.Domain.Leasing;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Leasing.Tenants;

public class TenantFamilyMemberCommandHandlerTests
{
    private readonly FakeTenantRepository _repository = new();
    private readonly FakeTenantContext _tenantContext = new();
    private readonly FakeCurrentUserContext _userContext = new();

    [Fact]
    public async Task CreateFamilyMember_ShouldAddRecord_WhenTenantExists()
    {
        // Arrange
        var companyId = _tenantContext.CompanyId!.Value;
        var tenant = Tenant.Create(companyId, "Ahmad", "1234567890", "+962791234567", DateTimeOffset.UtcNow, _userContext.UserId);
        _repository.Store.Add(tenant);

        var handler = new CreateTenantFamilyMemberCommandHandler(_repository, _tenantContext, _userContext);
        var command = new CreateTenantFamilyMemberCommand(tenant.Id, "Sami Ahmad", "Son", "Child");

        // Act
        var resultId = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, resultId);
        Assert.Single(_repository.AddedFamilyMembers);
        var added = _repository.AddedFamilyMembers[0];
        Assert.Equal("Sami Ahmad", added.Name);
        Assert.Equal("Son", added.RelationshipType);
        Assert.Equal("Child", added.AgeBracket);
        Assert.Equal(tenant.Id, added.TenantId);
        Assert.Equal(companyId, added.CompanyId);
    }

    [Fact]
    public async Task CreateFamilyMember_ShouldThrowNotFound_WhenTenantDoesNotExist()
    {
        // Arrange
        var handler = new CreateTenantFamilyMemberCommandHandler(_repository, _tenantContext, _userContext);
        var command = new CreateTenantFamilyMemberCommand(Guid.NewGuid(), "Sami Ahmad", "Son");

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateFamilyMember_ShouldUpdateDetails_WhenFamilyMemberExists()
    {
        // Arrange
        var companyId = _tenantContext.CompanyId!.Value;
        var tenantId = Guid.NewGuid();
        var member = TenantFamilyMember.Create(companyId, tenantId, "Old Name", "Son", DateTimeOffset.UtcNow, _userContext.UserId);
        _repository.FamilyMemberStore.Add(member);

        var handler = new UpdateTenantFamilyMemberCommandHandler(_repository, _tenantContext, _userContext);
        var command = new UpdateTenantFamilyMemberCommand(tenantId, member.Id, "New Name", "Spouse", "Adult");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("New Name", member.Name);
        Assert.Equal("Spouse", member.RelationshipType);
        Assert.Equal("Adult", member.AgeBracket);
    }

    [Fact]
    public async Task DeleteFamilyMember_ShouldSoftDelete_WhenFamilyMemberExists()
    {
        // Arrange
        var companyId = _tenantContext.CompanyId!.Value;
        var tenantId = Guid.NewGuid();
        var member = TenantFamilyMember.Create(companyId, tenantId, "Sami", "Son", DateTimeOffset.UtcNow, _userContext.UserId);
        _repository.FamilyMemberStore.Add(member);

        var handler = new DeleteTenantFamilyMemberCommandHandler(_repository, _tenantContext, _userContext);
        var command = new DeleteTenantFamilyMemberCommand(tenantId, member.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(member.DeletedAt);
        Assert.Equal(_userContext.UserId, member.DeletedBy);
    }

    [Fact]
    public async Task GetFamilyMembersForTenant_ShouldReturnList_WhenTenantExists()
    {
        // Arrange
        var companyId = _tenantContext.CompanyId!.Value;
        var tenant = Tenant.Create(companyId, "Ahmad", "1234567890", "+962791234567", DateTimeOffset.UtcNow, _userContext.UserId);
        _repository.Store.Add(tenant);

        var member1 = TenantFamilyMember.Create(companyId, tenant.Id, "Sami", "Son", DateTimeOffset.UtcNow, _userContext.UserId);
        var member2 = TenantFamilyMember.Create(companyId, tenant.Id, "Laila", "Daughter", DateTimeOffset.UtcNow, _userContext.UserId);
        _repository.FamilyMemberStore.Add(member1);
        _repository.FamilyMemberStore.Add(member2);

        var handler = new GetFamilyMembersForTenantQueryHandler(_repository, _tenantContext);
        var query = new GetFamilyMembersForTenantQuery(tenant.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Sami", result[0].Name);
        Assert.Equal("Laila", result[1].Name);
    }

    [Fact]
    public async Task GetFamilyMemberById_ShouldReturnDto_WhenExists()
    {
        // Arrange
        var companyId = _tenantContext.CompanyId!.Value;
        var tenantId = Guid.NewGuid();
        var member = TenantFamilyMember.Create(companyId, tenantId, "Sami", "Son", DateTimeOffset.UtcNow, _userContext.UserId);
        _repository.FamilyMemberStore.Add(member);

        var handler = new GetFamilyMemberByIdQueryHandler(_repository, _tenantContext);
        var query = new GetFamilyMemberByIdQuery(tenantId, member.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(member.Id, result!.Id);
        Assert.Equal("Sami", result.Name);
        Assert.Equal("Son", result.RelationshipType);
    }
}
