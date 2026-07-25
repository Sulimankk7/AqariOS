using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.DTOs.Identity;
using PropertyOS.Application.Identity.Commands.Register;

namespace PropertyOS.Application.Identity.Provisioning;

/// <summary>
/// Service interface handling the orchestration of tenant provisioning.
/// </summary>
public interface ITenantProvisioningService
{
    /// <summary>
    /// Executes the tenant provisioning pipeline steps for a new registration.
    /// </summary>
    /// <param name="command">Registration payload command.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Registration response with created IDs and tokens.</returns>
    Task<RegisterResponseDto> ProvisionTenantAsync(RegisterCommand command, CancellationToken cancellationToken = default);
}
