using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PropertyOS.Application.Common.Interfaces;
using PropertyOS.Application.Common.Security;
using PropertyOS.Domain.Identity.Entities;

namespace PropertyOS.Infrastructure.Identity;

/// <summary>
/// Infrastructure service that synchronizes the global platform RBAC permission catalog with PostgreSQL
/// and reconciles COMPANY_ADMIN role grants across tenants upon startup.
/// </summary>
public class PermissionCatalogSeeder : IPermissionCatalogSeeder
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<PermissionCatalogSeeder> _logger;

    /// <summary>
    /// Initializes a new instance of PermissionCatalogSeeder.
    /// </summary>
    public PermissionCatalogSeeder(IApplicationDbContext dbContext, ILogger<PermissionCatalogSeeder> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task SeedAndReconcileAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Platform RBAC Permission Catalog synchronization...");

        try
        {
            // 1. Synchronize Platform Permissions Catalog
            var existingPermissions = await _dbContext.Permissions
                .ToListAsync(cancellationToken);

            var existingMap = existingPermissions.ToDictionary(p => p.Key, StringComparer.OrdinalIgnoreCase);

            var now = DateTimeOffset.UtcNow;
            int newCount = 0;
            int updatedCount = 0;

            foreach (var catalogEntry in PlatformPermissions.Catalog)
            {
                if (!existingMap.TryGetValue(catalogEntry.Key, out var existing))
                {
                    var newPermission = new Permission
                    {
                        Id = Guid.CreateVersion7(),
                        Key = catalogEntry.Key,
                        Module = catalogEntry.Module,
                        DescriptionEn = catalogEntry.DescriptionEn,
                        DescriptionAr = catalogEntry.DescriptionAr,
                        IsDeprecated = false,
                        CreatedAt = now
                    };
                    _dbContext.Permissions.Add(newPermission);
                    existingMap[catalogEntry.Key] = newPermission;
                    newCount++;
                }
                else
                {
                    bool modified = false;
                    if (existing.Module != catalogEntry.Module)
                    {
                        existing.Module = catalogEntry.Module;
                        modified = true;
                    }
                    if (existing.DescriptionEn != catalogEntry.DescriptionEn)
                    {
                        existing.DescriptionEn = catalogEntry.DescriptionEn;
                        modified = true;
                    }
                    if (existing.DescriptionAr != catalogEntry.DescriptionAr)
                    {
                        existing.DescriptionAr = catalogEntry.DescriptionAr;
                        modified = true;
                    }
                    if (modified) updatedCount++;
                }
            }

            if (newCount > 0 || updatedCount > 0)
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Platform RBAC Permission Catalog synchronized: {NewCount} inserted, {UpdatedCount} updated.", newCount, updatedCount);
            }

            // 2. Reconcile SYSTEM / COMPANY_ADMIN Role Grants
            //
            // Set-based reconciliation: a single INSERT ... SELECT anti-join grants every
            // active (non-deprecated) permission to every live, company-scoped
            // COMPANY_ADMIN role that does not already hold it. This replaces the former
            // O(companies x permissions) tracked-entity loop, which materialized every
            // role-permission row into the change tracker on every startup.
            //
            // Semantics preserved exactly:
            //   - roles:        code = 'COMPANY_ADMIN', company_id IS NOT NULL, not soft-deleted
            //   - permissions:  is_deprecated = false
            //   - only missing (role_id, permission_id) pairs are inserted
            //   - granted_at = the same 'now' captured for the catalog sync; granted_by stays NULL
            //   - id falls back to the column default (uuid_generate_v7()), matching Guid.CreateVersion7()
            // ON CONFLICT DO NOTHING makes the statement race-safe against concurrent
            // seeders under the uq_role_permissions_role_permission unique index.
            int grantedCount;
            if (_dbContext.Database.IsRelational())
            {
                // Explicit transaction so TenantSessionInterceptor observes the same
                // transaction-started semantics as the previous SaveChangesAsync path.
                await using var reconcileTx = await _dbContext.BeginTransactionAsync(cancellationToken);
                grantedCount = await _dbContext.Database.ExecuteSqlRawAsync(
                    """
                    INSERT INTO role_permissions (role_id, permission_id, granted_at)
                    SELECT r.id, p.id, {0}
                    FROM roles r
                    CROSS JOIN permissions p
                    WHERE r.code = 'COMPANY_ADMIN'
                      AND r.company_id IS NOT NULL
                      AND r.deleted_at IS NULL
                      AND p.is_deprecated = false
                      AND NOT EXISTS (
                          SELECT 1 FROM role_permissions rp
                          WHERE rp.role_id = r.id AND rp.permission_id = p.id)
                    ON CONFLICT DO NOTHING
                    """,
                    new object[] { now },
                    cancellationToken);
                await reconcileTx.CommitAsync(cancellationToken);
            }
            else
            {
                // Non-relational provider (in-memory test double): raw SQL is unavailable,
                // so fall back to the tracked-entity reconciliation with identical semantics.
                var activePermissionIds = await _dbContext.Permissions
                    .Where(p => !p.IsDeprecated)
                    .Select(p => p.Id)
                    .ToListAsync(cancellationToken);

                var adminRoles = await _dbContext.Roles
                    .Include(r => r.RolePermissions)
                    .Where(r => r.Code == "COMPANY_ADMIN" && r.CompanyId != null && r.DeletedAt == null)
                    .ToListAsync(cancellationToken);

                grantedCount = 0;
                foreach (var role in adminRoles)
                {
                    var existingRolePermIds = role.RolePermissions.Select(rp => rp.PermissionId).ToHashSet();

                    foreach (var permId in activePermissionIds)
                    {
                        if (!existingRolePermIds.Contains(permId))
                        {
                            _dbContext.RolePermissions.Add(new RolePermission
                            {
                                Id = Guid.CreateVersion7(),
                                RoleId = role.Id,
                                PermissionId = permId,
                                GrantedAt = now
                            });
                            grantedCount++;
                        }
                    }
                }

                if (grantedCount > 0)
                {
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
            }

            if (grantedCount > 0)
            {
                _logger.LogInformation("Reconciled COMPANY_ADMIN role grants: {GrantedCount} new role-permission grants added.", grantedCount);
            }

            _logger.LogInformation("Platform RBAC Permission Catalog synchronization completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FATAL: Platform RBAC Permission Catalog synchronization failed. Failing application startup.");
            throw;
        }
    }
}
