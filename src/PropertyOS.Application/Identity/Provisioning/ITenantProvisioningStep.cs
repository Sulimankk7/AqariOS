using System.Threading;
using System.Threading.Tasks;

namespace PropertyOS.Application.Identity.Provisioning;

/// <summary>
/// Step interface representing an extensible task in the tenant provisioning pipeline.
/// </summary>
public interface ITenantProvisioningStep
{
    /// <summary>
    /// Execution order priority (lowest number executes first).
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Executes the provisioning step asynchronously.
    /// </summary>
    /// <param name="context">Tenant provisioning execution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ExecuteAsync(TenantProvisioningContext context, CancellationToken cancellationToken);
}
