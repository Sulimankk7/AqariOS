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
            var activePermissions = await _dbContext.Permissions
                .Where(p => !p.IsDeprecated)
                .ToListAsync(cancellationToken);

            var activePermissionIds = activePermissions.Select(p => p.Id).ToHashSet();

            var adminRoles = await _dbContext.Roles
                .Include(r => r.RolePermissions)
                .Where(r => r.Code == "COMPANY_ADMIN" && r.CompanyId != null && r.DeletedAt == null)
                .ToListAsync(cancellationToken);

            int grantedCount = 0;
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
                _logger.LogInformation("Reconciled COMPANY_ADMIN role grants: {GrantedCount} new role-permission grants added across {RoleCount} admin roles.", grantedCount, adminRoles.Count);
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
