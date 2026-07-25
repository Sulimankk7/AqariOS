using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using PropertyOS.Domain.Companies;
using PropertyOS.Domain.Identity.Entities;

namespace PropertyOS.Application.Common.Interfaces;

/// <summary>
/// Application database context interface exposing core entity DbSets and persistence operations.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Company> Companies { get; }
    DbSet<CompanySettings> CompanySettings { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserCompanyRole> UserCompanyRoles { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<LoginHistory> LoginHistory { get; }

    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
