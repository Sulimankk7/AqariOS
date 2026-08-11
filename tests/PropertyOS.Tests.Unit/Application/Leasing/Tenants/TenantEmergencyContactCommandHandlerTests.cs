using System;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Leasing.Commands.CreateTenantEmergencyContact;
using PropertyOS.Application.Leasing.Commands.DeleteTenantEmergencyContact;
using PropertyOS.Application.Leasing.Commands.UpdateTenantEmergencyContact;
using PropertyOS.Application.Leasing.Queries.GetEmergencyContactById;
using PropertyOS.Application.Leasing.Queries.GetEmergencyContactsForTenant;
using PropertyOS.Domain.Leasing;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Leasing.Tenants;

public class TenantEmergencyContactCommandHandlerTests
{
    private readonly FakeTenantRepository _repository = new();
    private readonly FakeTenantContext _tenantContext = new();
    private readonly FakeCurrentUserContext _userContext = new();

    [Fact]
    public async Task CreateEmergencyContact_ShouldAddRecord_WhenTenantExists()
    {
        // Arrange
        var companyId = _tenantContext.CompanyId!.Value;
        var tenant = Tenant.Create(companyId, "Ahmad", "1234567890", "+962791234567", DateTimeOffset.UtcNow, _userContext.UserId);
        _repository.Store.Add(tenant);

        var handler = new CreateTenantEmergencyContactCommandHandler(_repository, _tenantContext, _userContext);
        var command = new CreateTenantEmergencyContactCommand(tenant.Id, "Khaled Ahmad", "Brother", "+962798765432");

        // Act
        var resultId = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, resultId);
        Assert.Single(_repository.AddedEmergencyContacts);
        var added = _repository.AddedEmergencyContacts[0];
        Assert.Equal("Khaled Ahmad", added.Name);
        Assert.Equal("Brother", added.RelationshipType);
        Assert.Equal("+962798765432", added.Phone);
        Assert.Equal(tenant.Id, added.TenantId);
        Assert.Equal(companyId, added.CompanyId);
    }

    [Fact]
    public async Task CreateEmergencyContact_ShouldThrowNotFound_WhenTenantDoesNotExist()
    {
        // Arrange
        var handler = new CreateTenantEmergencyContactCommandHandler(_repository, _tenantContext, _userContext);
        var command = new CreateTenantEmergencyContactCommand(Guid.NewGuid(), "Khaled Ahmad", "Brother", "+962798765432");

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateEmergencyContact_ShouldUpdateDetails_WhenContactExists()
    {
        // Arrange
        var companyId = _tenantContext.CompanyId!.Value;
        var tenantId = Guid.NewGuid();
        var contact = TenantEmergencyContact.Create(companyId, tenantId, "Old Name", "Friend", "+962791111111", DateTimeOffset.UtcNow, _userContext.UserId);
        _repository.EmergencyContactStore.Add(contact);

        var handler = new UpdateTenantEmergencyContactCommandHandler(_repository, _tenantContext, _userContext);
        var command = new UpdateTenantEmergencyContactCommand(tenantId, contact.Id, "New Name", "Brother", "+962792222222");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("New Name", contact.Name);
        Assert.Equal("Brother", contact.RelationshipType);
        Assert.Equal("+962792222222", contact.Phone);
    }

    [Fact]
    public async Task DeleteEmergencyContact_ShouldSoftDelete_WhenContactExists()
    {
        // Arrange
        var companyId = _tenantContext.CompanyId!.Value;
        var tenantId = Guid.NewGuid();
        var contact = TenantEmergencyContact.Create(companyId, tenantId, "Khaled", "Brother", "+962791111111", DateTimeOffset.UtcNow, _userContext.UserId);
        _repository.EmergencyContactStore.Add(contact);

        var handler = new DeleteTenantEmergencyContactCommandHandler(_repository, _tenantContext, _userContext);
        var command = new DeleteTenantEmergencyContactCommand(tenantId, contact.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(contact.DeletedAt);
        Assert.Equal(_userContext.UserId, contact.DeletedBy);
    }

    [Fact]
    public async Task GetEmergencyContactsForTenant_ShouldReturnList_WhenTenantExists()
    {
        // Arrange
        var companyId = _tenantContext.CompanyId!.Value;
        var tenant = Tenant.Create(companyId, "Ahmad", "1234567890", "+962791234567", DateTimeOffset.UtcNow, _userContext.UserId);
        _repository.Store.Add(tenant);

        var contact1 = TenantEmergencyContact.Create(companyId, tenant.Id, "Khaled", "Brother", "+962791111111", DateTimeOffset.UtcNow, _userContext.UserId);
        var contact2 = TenantEmergencyContact.Create(companyId, tenant.Id, "Mariam", "Sister", "+962792222222", DateTimeOffset.UtcNow, _userContext.UserId);
        _repository.EmergencyContactStore.Add(contact1);
        _repository.EmergencyContactStore.Add(contact2);

        var handler = new GetEmergencyContactsForTenantQueryHandler(_repository, _tenantContext);
        var query = new GetEmergencyContactsForTenantQuery(tenant.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Khaled", result[0].Name);
        Assert.Equal("Mariam", result[1].Name);
    }

    [Fact]
    public async Task GetEmergencyContactById_ShouldReturnDto_WhenExists()
    {
        // Arrange
        var companyId = _tenantContext.CompanyId!.Value;
        var tenantId = Guid.NewGuid();
        var contact = TenantEmergencyContact.Create(companyId, tenantId, "Khaled", "Brother", "+962791111111", DateTimeOffset.UtcNow, _userContext.UserId);
        _repository.EmergencyContactStore.Add(contact);

        var handler = new GetEmergencyContactByIdQueryHandler(_repository, _tenantContext);
        var query = new GetEmergencyContactByIdQuery(tenantId, contact.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(contact.Id, result!.Id);
        Assert.Equal("Khaled", result.Name);
        Assert.Equal("Brother", result.RelationshipType);
        Assert.Equal("+962791111111", result.Phone);
    }
}
