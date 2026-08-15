using System.Threading;
using System.Threading.Tasks;

namespace PropertyOS.Application.Common.Interfaces;

/// <summary>
/// Service interface for sending transactional SMS messages across PropertyOS.
/// </summary>
public interface ISmsSender
{
    /// <summary>
    /// Sends an Arabic-first tenant portal activation SMS via Twilio Messaging Service.
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
