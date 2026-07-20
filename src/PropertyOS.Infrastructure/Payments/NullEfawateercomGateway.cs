using System.Threading;
using System.Threading.Tasks;
using PropertyOS.Application.Financials;

namespace PropertyOS.Infrastructure.Payments;

/// <summary>
/// No-op implementation of IEfawateercomGateway.
/// Used when the live eFAWATEERcom HTTP client is not configured (e.g. development,
/// integration tests, or when PollEfawateercomTransactionStatusCommand runs but the
/// gateway is not wired up).
///
/// Always returns null (transaction not found at provider).
/// This causes PollEfawateercomTransactionStatusCommand to log a warning and take no action.
/// </summary>
public sealed class NullEfawateercomGateway : IEfawateercomGateway
{
    public Task<EfawateercomGatewayStatusResult?> GetTransactionStatusAsync(
        string externalTransactionId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<EfawateercomGatewayStatusResult?>(null);
    }
}
