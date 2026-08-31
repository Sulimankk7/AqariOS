using System.Threading;
using System.Threading.Tasks;

namespace PropertyOS.Application.Common.Interfaces;

/// <summary>
/// Service interface for sending transactional SMS messages across PropertyOS.
/// </summary>
public interface ISmsSender
{
    /// <summary>
    /// Sends a transactional SMS through the configured provider.
    /// </summary>
    /// <param name="to">Destination phone number in international E.164 format.</param>
    /// <param name="message">SMS text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True when the provider accepts the SMS request; otherwise false.</returns>
    Task<bool> SendAsync(
        string to,
        string message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Legacy tenant-activation convenience operation retained for existing callers.
    /// </summary>
    /// <param name="recipientPhone">Destination E.164 phone number.</param>
    /// <param name="tenantName">Full name of the tenant.</param>
    /// <param name="activationToken">Raw activation token for generating the URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if SMS was successfully dispatched; false otherwise.</returns>
    Task<bool> SendTenantActivationSmsAsync(
        string recipientPhone,
        string tenantName,
        string activationToken,
        CancellationToken cancellationToken = default);
}
