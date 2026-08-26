namespace PropertyOS.Domain.Notifications.Enums;

public enum NotificationType
{
    NewLease,
    LeaseExpiration,
    RentDue,
    RentPaid,
    LatePayment,
    MaintenanceRequestCreated,
    MaintenanceRequestUpdated,
    MarketplaceViewingRequest,
    DocumentExpiring,
    GeneralNotification,

    // ── Utility Billing (Module 12) ──────────────────────────────────────────
    /// <summary>Tenant has a new outstanding electricity bill.</summary>
    UtilityBillElectricity,
    /// <summary>Tenant has a new outstanding water bill.</summary>
    UtilityBillWater
}
