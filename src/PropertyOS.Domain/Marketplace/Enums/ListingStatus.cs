namespace PropertyOS.Domain.Marketplace.Enums;

/// <summary>
/// Status of a marketplace listing. Maps to PostgreSQL listing_status_enum.
/// </summary>
public enum ListingStatus
{
    Draft,
    Published,
    Rented,
    Expired,
    Archived
}
