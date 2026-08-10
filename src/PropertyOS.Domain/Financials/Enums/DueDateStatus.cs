namespace PropertyOS.Domain.Financials.Enums;

public enum DueDateStatus
{
    Pending,
    PendingVerification,
    Paid,
    PartiallyPaid,
    Late,
    OverdueUnpaid,
    Cancelled
}
