using System;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Leasing.Commands.UpdateTenant;
using PropertyOS.Domain.Leasing;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Leasing.Tenants;

public class UpdateTenantCommandHandlerTests
{
    private static Tenant MakeTenant(Guid companyId) => Tenant.Create(
        companyId: companyId,
        name: "Original Name",
        nationalId: "1111111111",
        phone: "+962790000000",
        createdAt: DateTimeOffset.UtcNow,
        createdBy: Guid.NewGuid());

    private static UpdateTenantCommand CommandFor(Guid tenantId) => new(
        TenantId: tenantId,
        Name: "Updated Name",
        NationalId: "2222222222",
        Phone: "00962791234567",
        Email: " updated.email@example.com ",
        Occupation: "Teacher",
        Employer: "School");

    [Fact]
    public async Task Handle_MissingTenant_ThrowsNotFoundException()
    {
        var repo = new FakeTenantRepository();
        var handler = new UpdateTenantCommandHandler(repo, new FakeTenantContext(), new FakeCurrentUserContext());

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(CommandFor(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CrossTenantAccess_MaskedAsNotFound()
    {
        var repo = new FakeTenantRepository();
        var otherCompanyTenant = MakeTenant(Guid.NewGuid()); // belongs to a different company
        repo.Store.Add(otherCompanyTenant);

        var handler = new UpdateTenantCommandHandler(repo, new FakeTenantContext(), new FakeCurrentUserContext());

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(CommandFor(otherCompanyTenant.Id), CancellationToken.None));

        Assert.Equal("Original Name", otherCompanyTenant.Name); // untouched
    }

    [Fact]
    public async Task Handle_DuplicateNationalId_ThrowsConflictException_ExcludingSelf()
    {
        var tenantCtx = new FakeTenantContext();
        var repo = new FakeTenantRepository { NationalIdExists = true };
        var tenant = MakeTenant(tenantCtx.CompanyId!.Value);
        repo.Store.Add(tenant);

        var handler = new UpdateTenantCommandHandler(repo, tenantCtx, new FakeCurrentUserContext());

        await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(CommandFor(tenant.Id), CancellationToken.None));

        Assert.Equal(tenant.Id, repo.LastExistsExcludeTenantId); // self-exclusion applied
        Assert.Equal("Original Name", tenant.Name);              // untouched
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesTenantDetails()
    {
        var tenantCtx = new FakeTenantContext();
        var userCtx = new FakeCurrentUserContext();
        var repo = new FakeTenantRepository();
        var tenant = MakeTenant(tenantCtx.CompanyId!.Value);
        repo.Store.Add(tenant);

        var handler = new UpdateTenantCommandHandler(repo, tenantCtx, userCtx);

        await handler.Handle(CommandFor(tenant.Id), CancellationToken.None);

        Assert.Equal("Updated Name", tenant.Name);
        Assert.Equal("2222222222", tenant.NationalId);
        Assert.Equal("+962791234567", tenant.Phone); // E.164-normalized
        Assert.Equal("updated.email@example.com", tenant.Email);
        Assert.Equal("Teacher", tenant.Occupation);
        Assert.Equal("School", tenant.Employer);
        Assert.Equal(userCtx.UserId, tenant.UpdatedBy);
    }

    [Fact]
    public async Task Handle_AnotherActiveTenantsPhone_ThrowsConflict()
    {
        var tenantCtx = new FakeTenantContext();
        var repo = new FakeTenantRepository();
        var tenant = MakeTenant(tenantCtx.CompanyId!.Value);
        var other = Tenant.Create(
            Guid.NewGuid(), "Other", "3333333333", "+962791234567", DateTimeOffset.UtcNow, null);
        repo.Store.Add(tenant);
        repo.Store.Add(other);
        var handler = new UpdateTenantCommandHandler(repo, tenantCtx, new FakeCurrentUserContext());

        await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(CommandFor(tenant.Id), CancellationToken.None));

        Assert.Equal("Original Name", tenant.Name);
    }

    [Fact]
    public async Task Handle_KeepingOwnCanonicalPhone_IsAllowed()
    {
        var tenantCtx = new FakeTenantContext();
        var repo = new FakeTenantRepository();
        var tenant = MakeTenant(tenantCtx.CompanyId!.Value);
        repo.Store.Add(tenant);
        var handler = new UpdateTenantCommandHandler(repo, tenantCtx, new FakeCurrentUserContext());
        var command = new UpdateTenantCommand(
            tenant.Id, "Updated", "1111111111", "+962 790 000 000");

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal("+962790000000", tenant.Phone);
        Assert.Equal(tenant.Id, repo.LastExistsPhoneExcludeTenantId);
    }
}
