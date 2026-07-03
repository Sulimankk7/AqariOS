namespace PropertyOS.Domain.Common;

/// <summary>
/// Applied to entities that support soft-delete semantics
/// (deleted_at / deleted_by per Global Conventions).
///
/// NOT applied to:
///   • Append-only event logs (meter_readings, login_history, audit_logs,
///     contract_status_history, maintenance_status_history).
///   • No-independent-lifecycle 1:1 extensions (company_settings,
///     building_addresses, company_receipt_sequences).
///   • notification_deliveries — must never be deleted.
/// </summary>
public interface ISoftDeletable
{
    DateTimeOffset? DeletedAt { get; }
    Guid? DeletedBy { get; }
}
