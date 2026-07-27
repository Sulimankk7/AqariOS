using System;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Leasing.Commands.DeleteTenant;
using PropertyOS.Domain.Leasing;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Leasing.Tenants;

public class DeleteTenantCommandHandlerTests
{
    private static Tenant MakeTenant(Guid companyId) => Tenant.Create(
        companyId: companyId,
        name: "Ahmad Odeh",
        nationalId: "9901234567",
        phone: "+962791234567",
        createdAt: DateTimeOffset.UtcNow,
        createdBy: Guid.NewGuid());

    [Fact]
    public async Task Handle_MissingTenant_ThrowsNotFoundException()
    {
        var repo = new FakeTenantRepository();
        var handler = new DeleteTenantCommandHandler(repo, new FakeTenantContext(), new FakeCurrentUserContext());

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new DeleteTenantCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CrossTenantAccess_MaskedAsNotFound()
    {
        var repo = new FakeTenantRepository();
        var otherCompanyTenant = MakeTenant(Guid.NewGuid());
        repo.Store.Add(otherCompanyTenant);

        var handler = new DeleteTenantCommandHandler(repo, new FakeTenantContext(), new FakeCurrentUserContext());

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new DeleteTenantCommand(otherCompanyTenant.Id), CancellationToken.None));

        Assert.Null(otherCompanyTenant.DeletedAt);
    }

    [Fact]
    public async Task Handle_TenantWithNonTerminalLease_ThrowsBusinessRuleException()
    {
        var tenantCtx = new FakeTenantContext();
        var repo = new FakeTenantRepository { HasNonTerminalLease = true };
        var tenant = MakeTenant(tenantCtx.CompanyId!.Value);
        repo.Store.Add(tenant);

        var handler = new DeleteTenantCommandHandler(repo, tenantCtx, new FakeCurrentUserContext());

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => handler.Handle(new DeleteTenantCommand(tenant.Id), CancellationToken.None));

        Assert.Equal("TENANT_HAS_ACTIVE_LEASE", ex.Code);
        Assert.Equal(tenant.Id, repo.LastNonTerminalLeaseTenantId);
        Assert.Null(tenant.DeletedAt);
    }

    [Fact]
    public async Task Handle_ValidCommand_SoftDeletesTenant()
    {
        var tenantCtx = new FakeTenantContext();
        var userCtx = new FakeCurrentUserContext();
        var repo = new FakeTenantRepository();
        var tenant = MakeTenant(tenantCtx.CompanyId!.Value);
        repo.Store.Add(tenant);

        var handler = new DeleteTenantCommandHandler(repo, tenantCtx, userCtx);

        await handler.Handle(new DeleteTenantCommand(tenant.Id), CancellationToken.None);

        Assert.NotNull(tenant.DeletedAt);
        Assert.Equal(userCtx.UserId, tenant.DeletedBy);
    }
}
