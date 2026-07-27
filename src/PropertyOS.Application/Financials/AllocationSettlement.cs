using System;
using PropertyOS.Domain.Financials.Enums;

namespace PropertyOS.Application.Financials;

/// <summary>
/// Single source of truth for deriving an obligation's cached settlement state
/// (rent_payments.due_date_status) from its active-allocation total, per the
/// grace-period-aware matrix in Module 6 doc §6.1. Used by allocation recording,
/// allocation reversal, the cheque bounce/cancel cascade, and the overdue sweep.
///
/// Matrix (effectiveDue = dueDate + graceDays; company_settings.rent_grace_period_days):
///   paid >= due                          -> Paid
///   0 &lt; paid &lt; due, today &lt;= effectiveDue -> PartiallyPaid
///   0 &lt; paid &lt; due, today &gt;  effectiveDue -> Late          (paid something, past grace)
///   paid = 0,       today &lt;= effectiveDue -> Pending
///   paid = 0,       today &gt;  effectiveDue -> OverdueUnpaid (paid nothing, past grace)
///   dueDate == null                      -> Pending/PartiallyPaid/Paid only (never late/overdue)
/// Cancelled is sticky and never derived — callers must skip Cancelled rows entirely.
/// </summary>
public static class AllocationSettlement
{
    public static DueDateStatus DeriveStatus(decimal amountPaid, decimal amountDue, DateOnly? dueDate, DateOnly today, int graceDays)
    {
        if (amountPaid >= amountDue)
            return DueDateStatus.Paid;

        var pastGraceAdjustedDueDate = dueDate.HasValue && today > dueDate.Value.AddDays(graceDays);

        if (amountPaid > 0)
            return pastGraceAdjustedDueDate ? DueDateStatus.Late : DueDateStatus.PartiallyPaid;

        return pastGraceAdjustedDueDate ? DueDateStatus.OverdueUnpaid : DueDateStatus.Pending;
    }
}
