namespace PropertyOS.Domain.Properties.Enums;

/// <summary>
/// Apartment ownership model. Maps to PostgreSQL enum <c>ownership_status_enum</c>.
/// Approved values per Module 4 §4.4.
/// </summary>
public enum OwnershipStatus
{
    CompanyOwned,
    ThirdPartyOwned,
}
