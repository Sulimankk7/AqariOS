using System;
using MediatR;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Financials.Commands.CancelEfawateercomTransaction;

/// <summary>
/// Operator-initiated cancellation of a non-terminal eFAWATEERcom transaction.
/// Valid only when the transaction is still in Pending or Sent state.
/// Idempotent: calling this on an already-terminal transaction is a safe no-op.
/// </summary>
public record CancelEfawateercomTransactionCommand(
    Guid TransactionId,
    string? Reason = null
) : ICommand;
