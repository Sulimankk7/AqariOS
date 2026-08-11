using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PropertyOS.Application.Common.Exceptions;
using PropertyOS.Application.Leasing.Commands.ProvisionTenantAccount;
using PropertyOS.Domain.Identity.Entities;
using PropertyOS.Domain.Leasing;
using PropertyOS.Infrastructure.Persistence;
using Xunit;

namespace PropertyOS.Tests.Unit.Application.Leasing.Tenants;

public class ProvisionTenantAccountCommandHandlerTests
{
    private static PropertyOsDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<PropertyOsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new PropertyOsDbContext(options);
    }

    [Fact]
    public async Task Handle_ValidTenant_ProvisionsUserAccountAndLinksTenantUserId()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var companyId = Guid.NewGuid();

        // Seed system TENANT role
        var tenantRole = new Role
        {
            Id = Guid.NewGuid(),
            Code = "TENANT",
            NameEn = "Tenant",
            NameAr = "مستأجر",
            IsSystem = true
        };
        dbContext.Roles.Add(tenantRole);

        // Seed Tenant aggregate
        var tenant = Tenant.Create(
            companyId: companyId,
            name: "Sami Al-Khatib",
            nationalId: "9801234567",
            phone: "+962791112223",
            createdAt: DateTimeOffset.UtcNow,
            createdBy: Guid.NewGuid()
        );
        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync();

        var repo = new FakeTenantRepository { Store = { tenant } };
        var tenantCtx = new FakeTenantContext { CompanyId = companyId };

        var handler = new ProvisionTenantAccountCommandHandler(repo, tenantCtx, dbContext);
        var command = new ProvisionTenantAccountCommand(tenant.Id, Email: "sami@example.com");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TenantId.Should().Be(tenant.Id);
        result.UserId.Should().NotBeEmpty();
        result.CompanyId.Should().Be(companyId);
        result.ActivationToken.Should().NotBeNullOrWhiteSpace();

        // Verify Tenant.UserId is updated
        tenant.UserId.Should().Be(result.UserId);

        // Verify User record added to db
        var createdUser = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == result.UserId);
        createdUser.Should().NotBeNull();
        createdUser!.FullName.Should().Be("Sami Al-Khatib");
        createdUser.Phone.Should().Be("+962791112223");
        createdUser.Email.Should().Be("sami@example.com");
        createdUser.PasswordHash.Should().BeNull(); // Until activation
        createdUser.PasswordResetTokenHash.Should().NotBeNullOrWhiteSpace();

        // Verify UserCompanyRole added
        var membership = await dbContext.UserCompanyRoles.FirstOrDefaultAsync(m => m.UserId == result.UserId);
        membership.Should().NotBeNull();
        membership!.CompanyId.Should().Be(companyId);
        membership.RoleId.Should().Be(tenantRole.Id);
    }

    [Fact]
    public async Task Handle_AlreadyProvisionedTenant_ThrowsConflictException()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var companyId = Guid.NewGuid();
        var tenant = Tenant.Create(companyId, "Linked Tenant", "9998887776", "+962799988776", DateTimeOffset.UtcNow, Guid.NewGuid(), userId: Guid.NewGuid());
        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync();

        var repo = new FakeTenantRepository { Store = { tenant } };
        var tenantCtx = new FakeTenantContext { CompanyId = companyId };

        var handler = new ProvisionTenantAccountCommandHandler(repo, tenantCtx, dbContext);
        var command = new ProvisionTenantAccountCommand(tenant.Id);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*TENANT_ACCOUNT_ALREADY_EXISTS*");
    }

    [Fact]
    public async Task Handle_CrossCompanyTenant_ThrowsNotFoundException()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();

        var tenant = Tenant.Create(companyB, "Company B Tenant", "9990001112", "+962790001112", DateTimeOffset.UtcNow, Guid.NewGuid());
        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync();

        var repo = new FakeTenantRepository { Store = { tenant } };
        var tenantCtx = new FakeTenantContext { CompanyId = companyA }; // Context is Company A

        var handler = new ProvisionTenantAccountCommandHandler(repo, tenantCtx, dbContext);
        var command = new ProvisionTenantAccountCommand(tenant.Id);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_DuplicateUserEmail_ThrowsConflictException()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var companyId = Guid.NewGuid();
        var existingEmail = "existing@example.com";

        dbContext.Users.Add(new User { Id = Guid.NewGuid(), Email = existingEmail, FullName = "Existing User" });
        await dbContext.SaveChangesAsync();

        var tenant = Tenant.Create(companyId, "New Tenant", "9991112223", "+962791112224", DateTimeOffset.UtcNow, Guid.NewGuid());
        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync();

        var repo = new FakeTenantRepository { Store = { tenant } };
        var tenantCtx = new FakeTenantContext { CompanyId = companyId };

        var handler = new ProvisionTenantAccountCommandHandler(repo, tenantCtx, dbContext);
        var command = new ProvisionTenantAccountCommand(tenant.Id, Email: existingEmail);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage($"*{existingEmail}*");
    }

    [Fact]
    public async Task Handle_DuplicateUserPhone_ThrowsConflictException()
    {
        // Arrange
        using var dbContext = CreateInMemoryDbContext();
        var companyId = Guid.NewGuid();
        var existingPhone = "+962791112223";

        dbContext.Users.Add(new User { Id = Guid.NewGuid(), Phone = existingPhone, FullName = "Existing User" });
        await dbContext.SaveChangesAsync();

        var tenant = Tenant.Create(companyId, "New Tenant", "9991112223", existingPhone, DateTimeOffset.UtcNow, Guid.NewGuid());
        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync();

        var repo = new FakeTenantRepository { Store = { tenant } };
        var tenantCtx = new FakeTenantContext { CompanyId = companyId };

        var handler = new ProvisionTenantAccountCommandHandler(repo, tenantCtx, dbContext);
        var command = new ProvisionTenantAccountCommand(tenant.Id);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage($"*{existingPhone}*");
    }
}
