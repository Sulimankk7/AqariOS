namespace PropertyOS.Domain.UtilityBills.Enums;

/// <summary>
/// The payment status of a utility bill as reported by the external provider.
/// Maps to the PostgreSQL enum type: utility_bill_status_enum.
///
/// Unknown is used when the provider returns a bill record but does not
/// explicitly report whether it has been paid.
/// </summary>
public enum UtilityBillPaymentStatus
{
    Unpaid,
    Paid,
    Unknown
}
