namespace PropertyOS.Domain.Marketplace.Enums;

/// <summary>
/// Status of a marketplace viewing request. Maps to PostgreSQL viewing_request_status_enum.
/// </summary>
public enum ViewingRequestStatus
{
    Pending,
    Contacted,
    Scheduled,
    Completed,
    Cancelled
}
