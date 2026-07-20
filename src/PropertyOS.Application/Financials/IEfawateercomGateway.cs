using System;
using System.Threading;
using System.Threading.Tasks;

namespace PropertyOS.Application.Financials;

/// <summary>
/// Outbound gateway abstraction for the eFAWATEERcom payment provider.
///
/// This interface belongs in the Application layer so that command handlers can depend on
/// it without referencing Infrastructure. The concrete HTTP implementation lives in
/// Infrastructure.Payments. A null implementation (NullEfawateercomGateway) is registered
/// when the gateway is not yet configured or when running integration tests.
///
/// Design contract:
///   • Implementations MUST NOT throw on provider-side failures — they return a typed result.
///   • Implementations MUST NOT perform any business logic — they are pure I/O adapters.
///   • All network timeouts must be caught and surfaced via EfawateercomGatewayResult.
/// </summary>
public interface IEfawateercomGateway
{
    /// <summary>
    /// Queries the eFAWATEERcom API for the current status of a transaction.
    /// Used as a polling fallback when the callback webhook was not received.
    /// Returns null if the external transaction ID is not recognized by the provider.
    /// </summary>
    Task<EfawateercomGatewayStatusResult?> GetTransactionStatusAsync(
        string externalTransactionId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result returned by IEfawateercomGateway.GetTransactionStatusAsync.
/// Maps the provider's raw response to structured fields.
/// </summary>
public sealed record EfawateercomGatewayStatusResult(
    Domain.Financials.Enums.EfawateercomStatus Status,
    DateTimeOffset ResponseTime,
    string? ResponseCode,
    string? ResponseMessage,
    string? RawResponse
);
