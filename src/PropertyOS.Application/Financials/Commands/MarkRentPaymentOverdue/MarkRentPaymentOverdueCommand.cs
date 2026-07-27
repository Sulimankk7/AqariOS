using System;
using PropertyOS.Application.Common.Interfaces;

namespace PropertyOS.Application.Financials.Commands.MarkRentPaymentOverdue;

/// <summary>
/// Recomputes the cached settlement status (rent_payments.due_date_status) of a single
/// scheduled installment whose due date has passed relative to the Jordan business
/// calendar (Asia/Amman). Intended to be dispatched by MarkOverdueRentPaymentsJob.
/// Idempotent: payments that are not Pending scheduled installments are silent no-ops.
/// </summary>
/// <param name="RentPaymentId">The rent payment (scheduled installment) to evaluate.</param>
/// <param name="AsOf">
/// Optional override for the reference UTC instant used to derive the Jordan business
/// date. When null, the current business clock time is used.
/// </param>
public record MarkRentPaymentOverdueCommand(
    Guid RentPaymentId,
    DateTimeOffset? AsOf = null
) : ICommand;
