using System.Threading;
using System.Threading.Tasks;

namespace PropertyOS.Application.Common.Interfaces;

/// <summary>
/// Service interface for sending transactional emails across PropertyOS.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Sends an Arabic-first tenant portal activation email via transactional email gateway.
    /// </summary>
    /// <param name="recipientEmail">Destination email address.</param>
    /// <param name="tenantName">Full name of the tenant.</param>
    /// <param name="activationToken">Raw activation token for generating the URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if email was successfully dispatched; false otherwise.</returns>
    Task<bool> SendTenantActivationEmailAsync(
        string recipientEmail,
        string tenantName,
        string activationToken,
        CancellationToken cancellationToken = default);
}
