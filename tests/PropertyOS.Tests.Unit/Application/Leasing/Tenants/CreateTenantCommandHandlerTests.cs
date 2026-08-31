using System;
using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Leasing.Commands.CreateTenant;
using PropertyOS.Domain.Leasing;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Leasing.Tenants;

public class CreateTenantCommandHandlerTests
{
    private static CreateTenantCommand ValidCommand() => new(
        Name: "Ahmad Odeh",
        NationalId: " 9901234567 ",
        Phone: "0096279 123 4567",
        Email: " Ahmad.Odeh@Example.com ",
        Occupation: "Engineer",
        Employer: "Acme");

    [Fact]
    public async Task Handle_ValidCommand_AddsTenantAndReturnsNonEmptyId()
    {
        var repo = new FakeTenantRepository();
        var tenantCtx = new FakeTenantContext();
        var userCtx = new FakeCurrentUserContext();
        var handler = new CreateTenantCommandHandler(repo, tenantCtx, userCtx);

        var returnedId = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Single(repo.AddedTenants);
        var tenant = repo.AddedTenants[0];

        Assert.NotEqual(Guid.Empty, tenant.Id); // Client-generated UUIDv7 (available pre-save)
        Assert.Equal(tenant.Id, returnedId);
        Assert.Equal(tenantCtx.CompanyId, tenant.CompanyId);
        Assert.Equal("Ahmad Odeh", tenant.Name);
        Assert.Equal("9901234567", tenant.NationalId);       // trimmed
        Assert.Equal("+962791234567", tenant.Phone);          // E.164-normalized
        Assert.Equal("ahmad.odeh@example.com", tenant.Email); // trimmed & lowercased
        Assert.Equal("Engineer", tenant.Occupation);
        Assert.Equal("Acme", tenant.Employer);
        Assert.Null(tenant.UserId);                           // account/invitation flow deferred
        Assert.Equal(userCtx.UserId, tenant.CreatedBy);
    }

    [Fact]
    public async Task Handle_DuplicateNationalId_ThrowsConflictException()
    {
        var repo = new FakeTenantRepository { NationalIdExists = true };
        var tenantCtx = new FakeTenantContext();
        var handler = new CreateTenantCommandHandler(repo, tenantCtx, new FakeCurrentUserContext());

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(ValidCommand(), CancellationToken.None));

        Assert.Empty(repo.AddedTenants);
        Assert.Equal(tenantCtx.CompanyId, repo.LastExistsCompanyId);
        Assert.Equal("9901234567", repo.LastExistsNationalId); // trimmed before uniqueness check
        Assert.Null(repo.LastExistsExcludeTenantId);
    }

    [Fact]
    public async Task Handle_ExactDuplicateActivePhone_ThrowsSafeConflict()
    {
        var repo = new FakeTenantRepository { PhoneExistsOverride = true };
        var handler = new CreateTenantCommandHandler(repo, new FakeTenantContext(), new FakeCurrentUserContext());

        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => handler.Handle(ValidCommand() with { Phone = "+962791234567" }, CancellationToken.None));

        Assert.Equal("An active tenant with this phone number already exists.", exception.Message);
        Assert.Equal("+962791234567", repo.LastExistsPhone);
        Assert.Null(repo.LastExistsPhoneExcludeTenantId);
        Assert.Empty(repo.AddedTenants);
    }

    [Fact]
    public async Task Handle_FormattedVariantOfExistingPhone_ThrowsConflictAfterCanonicalization()
    {
        var repo = new FakeTenantRepository();
        repo.Store.Add(Tenant.Create(
            Guid.NewGuid(), "Existing", "EXISTING", "+962791234567", DateTimeOffset.UtcNow, null));
        var handler = new CreateTenantCommandHandler(repo, new FakeTenantContext(), new FakeCurrentUserContext());

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
            ValidCommand() with { Phone = "+962 79 123 4567" }, CancellationToken.None));

        Assert.Equal("+962791234567", repo.LastExistsPhone);
        Assert.Empty(repo.AddedTenants);
    }

    [Fact]
    public async Task Handle_DifferentInternationalPhone_RemainsDistinctAndSucceeds()
    {
        var repo = new FakeTenantRepository();
        repo.Store.Add(Tenant.Create(
            Guid.NewGuid(), "Saudi Tenant", "SA-1", "+966551234567", DateTimeOffset.UtcNow, null));
        var handler = new CreateTenantCommandHandler(repo, new FakeTenantContext(), new FakeCurrentUserContext());

        await handler.Handle(
            ValidCommand() with { Phone = "+971 50 123 4567" }, CancellationToken.None);

        Assert.Single(repo.AddedTenants);
        Assert.Equal("+971501234567", repo.AddedTenants[0].Phone);
    }

    [Fact]
    public async Task Handle_SoftDeletedTenantPhone_CanBeReused()
    {
        var repo = new FakeTenantRepository();
        var deleted = Tenant.Create(
            Guid.NewGuid(), "Deleted", "OLD", "+962791234567", DateTimeOffset.UtcNow, null);
        deleted.SoftDelete(DateTimeOffset.UtcNow, null);
        repo.Store.Add(deleted);
        var handler = new CreateTenantCommandHandler(repo, new FakeTenantContext(), new FakeCurrentUserContext());

        await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.Single(repo.AddedTenants);
    }
}
