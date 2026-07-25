using System.Threading;
using System.Threading.Tasks;

namespace PropertyOS.Application.Common.Interfaces;

/// <summary>
/// Service interface for synchronizing the platform RBAC permission catalog with PostgreSQL database
/// and reconciling system role grants.
/// </summary>
public interface IPermissionCatalogSeeder
{
    /// <summary>
    /// Synchronizes global permissions and reconciles COMPANY_ADMIN role permissions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SeedAndReconcileAsync(CancellationToken cancellationToken = default);
}
