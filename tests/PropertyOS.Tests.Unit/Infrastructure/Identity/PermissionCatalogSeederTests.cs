using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PropertyOS.Application.Common.Security;
using PropertyOS.Domain.Identity.Entities;
using PropertyOS.Infrastructure.Identity;
using PropertyOS.Infrastructure.Persistence;
using Xunit;

namespace PropertyOS.Tests.Unit.Infrastructure.Identity;

public class PermissionCatalogSeederTests
{
    private PropertyOsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PropertyOsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new PropertyOsDbContext(options);
    }

    [Fact]
    public async Task SeedAndReconcileAsync_ShouldSeedAllPlatformPermissions_WhenDatabaseIsEmpty()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var seeder = new PermissionCatalogSeeder(dbContext, NullLogger<PermissionCatalogSeeder>.Instance);

        // Act
        await seeder.SeedAndReconcileAsync();

        // Assert
        var permissions = await dbContext.Permissions.ToListAsync();
        permissions.Should().HaveCount(PlatformPermissions.Catalog.Count);

        foreach (var catalogItem in PlatformPermissions.Catalog)
        {
            var seeded = permissions.FirstOrDefault(p => p.Key == catalogItem.Key);
            seeded.Should().NotBeNull();
            seeded!.Module.Should().Be(catalogItem.Module);
            seeded.DescriptionEn.Should().Be(catalogItem.DescriptionEn);
            seeded.DescriptionAr.Should().Be(catalogItem.DescriptionAr);
            seeded.IsDeprecated.Should().BeFalse();
        }
    }

    [Fact]
    public async Task SeedAndReconcileAsync_ShouldBeIdempotent_AndNotDuplicatePermissionsOnReRun()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var seeder = new PermissionCatalogSeeder(dbContext, NullLogger<PermissionCatalogSeeder>.Instance);

        // Act
        await seeder.SeedAndReconcileAsync();
        await seeder.SeedAndReconcileAsync();

        // Assert
        var permissions = await dbContext.Permissions.ToListAsync();
        permissions.Should().HaveCount(PlatformPermissions.Catalog.Count);
    }

    [Fact]
    public async Task SeedAndReconcileAsync_ShouldReconcileCompanyAdminRoles_WithActivePermissions()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var seeder = new PermissionCatalogSeeder(dbContext, NullLogger<PermissionCatalogSeeder>.Instance);

        var companyId = Guid.CreateVersion7();
        var adminRole = new Role
        {
            Id = Guid.CreateVersion7(),
            CompanyId = companyId,
            Code = "COMPANY_ADMIN",
            NameEn = "Company Administrator",
            NameAr = "مدير الشركة",
            IsSystem = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var customRole = new Role
        {
            Id = Guid.CreateVersion7(),
            CompanyId = companyId,
            Code = "MAINTENANCE_TECH",
            NameEn = "Maintenance Tech",
            NameAr = "فني صيانة",
            IsSystem = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Roles.AddRange(adminRole, customRole);
        await dbContext.SaveChangesAsync();

        // Act
        await seeder.SeedAndReconcileAsync();

        // Assert
        var adminGrants = await dbContext.RolePermissions.Where(rp => rp.RoleId == adminRole.Id).ToListAsync();
        adminGrants.Should().HaveCount(PlatformPermissions.Catalog.Count(p => !PlatformPermissions.IsPlatformOnly(p.Key)));
        var platformPermissionIds = dbContext.Permissions
            .Where(p => p.Key.StartsWith("platform."))
            .Select(p => p.Id)
            .ToHashSet();
        adminGrants.Should().OnlyContain(g => !platformPermissionIds.Contains(g.PermissionId));

        var customGrants = await dbContext.RolePermissions.Where(rp => rp.RoleId == customRole.Id).ToListAsync();
        customGrants.Should().BeEmpty("Custom non-admin roles must not automatically receive permissions during reconciliation");
    }

    [Fact]
    public async Task SeedAndReconcileAsync_ShouldGrantNewlyAddedPlatformPermissions_ToExistingCompanyAdminRoles()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var seeder = new PermissionCatalogSeeder(dbContext, NullLogger<PermissionCatalogSeeder>.Instance);

        // 1. Initial seed
        await seeder.SeedAndReconcileAsync();

        var adminRole = new Role
        {
            Id = Guid.CreateVersion7(),
            CompanyId = Guid.CreateVersion7(),
            Code = "COMPANY_ADMIN",
            NameEn = "Company Administrator",
            NameAr = "مدير الشركة",
            IsSystem = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        dbContext.Roles.Add(adminRole);
        await dbContext.SaveChangesAsync();

        // 2. Re-run reconciliation
        await seeder.SeedAndReconcileAsync();

        // Assert
        var adminGrants = await dbContext.RolePermissions.Where(rp => rp.RoleId == adminRole.Id).ToListAsync();
        adminGrants.Should().HaveCount(PlatformPermissions.Catalog.Count(p => !PlatformPermissions.IsPlatformOnly(p.Key)));
    }

    [Fact]
    public async Task SeedAndReconcileAsync_ShouldNotGrantPermissionsToDeletedCompanyAdminRoles()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var seeder = new PermissionCatalogSeeder(dbContext, NullLogger<PermissionCatalogSeeder>.Instance);

        var deletedAdminRole = new Role
        {
            Id = Guid.CreateVersion7(),
            CompanyId = Guid.CreateVersion7(),
            Code = "COMPANY_ADMIN",
            NameEn = "Company Administrator",
            NameAr = "مدير الشركة",
            IsSystem = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            DeletedAt = DateTimeOffset.UtcNow
        };
        dbContext.Roles.Add(deletedAdminRole);
        await dbContext.SaveChangesAsync();

        // Act
        await seeder.SeedAndReconcileAsync();

        // Assert
        var grants = await dbContext.RolePermissions.Where(rp => rp.RoleId == deletedAdminRole.Id).ToListAsync();
        grants.Should().BeEmpty("Soft-deleted admin roles must not receive automatic permission grants");
    }

    [Fact]
    public async Task SeedAndReconcileAsync_ShouldNotGrantPermissionsToRolesWithoutCompanyId()
    {
        // Arrange
        using var dbContext = CreateDbContext();
        var seeder = new PermissionCatalogSeeder(dbContext, NullLogger<PermissionCatalogSeeder>.Instance);

        var globalRole = new Role
        {
            Id = Guid.CreateVersion7(),
            CompanyId = null,
            Code = "COMPANY_ADMIN",
            NameEn = "Global System Role",
            NameAr = "دور عام",
            IsSystem = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        dbContext.Roles.Add(globalRole);
        await dbContext.SaveChangesAsync();

        // Act
        await seeder.SeedAndReconcileAsync();

        // Assert
        var grants = await dbContext.RolePermissions.Where(rp => rp.RoleId == globalRole.Id).ToListAsync();
        grants.Should().BeEmpty("Global roles without a CompanyId must not receive automatic tenant admin grants");
    }

    [Fact]
    public async Task SeedAndReconcileAsync_ShouldCreateSystemAdminWithOnlyPlatformPermissions()
    {
        using var dbContext = CreateDbContext();
        var seeder = new PermissionCatalogSeeder(dbContext, NullLogger<PermissionCatalogSeeder>.Instance);

        await seeder.SeedAndReconcileAsync();

        var role = await dbContext.Roles.SingleAsync(r => r.Code == PlatformRoles.SystemAdmin);
        role.CompanyId.Should().BeNull();
        role.IsSystem.Should().BeTrue();

        var grantedKeys = await (from rp in dbContext.RolePermissions
                                 join permission in dbContext.Permissions on rp.PermissionId equals permission.Id
                                 where rp.RoleId == role.Id
                                 select permission.Key).ToListAsync();

        grantedKeys.Should().BeEquivalentTo(
            PlatformPermissions.Catalog.Where(p => PlatformPermissions.IsPlatformOnly(p.Key)).Select(p => p.Key));
    }
}
