using System;
using MediatR;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Financials.Commands.PollEfawateercomTransactionStatus;

/// <summary>
/// Polls the eFAWATEERcom gateway for the current status of a non-terminal transaction
/// and processes the result through the same pipeline as an inbound callback.
///
/// Use cases:
///   1. A background job detects that a transaction has been in Sent state for longer
///      than the expected callback window (e.g. 15 minutes).
///   2. An operator manually triggers a status refresh via the admin API.
///
/// If the gateway returns a result, this command internally dispatches
/// ReceiveEfawateercomCallbackCommand so the full settlement pipeline (allocation +
/// receipt issuance) is reused without duplication.
///
/// If the gateway returns null (transaction not found at provider), this command
/// is a no-op — no state change is made.
/// </summary>
public record PollEfawateercomTransactionStatusCommand(
    string ExternalTransactionId
) : ICommand;
