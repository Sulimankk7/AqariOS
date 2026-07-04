namespace PropertyOS.Domain.Properties.Enums;

/// <summary>
/// Building classification. Maps to PostgreSQL enum <c>building_type_enum</c>.
/// Approved values per Module 4 §4.1.
/// </summary>
public enum BuildingType
{
    Residential,
    Commercial,
    MixedUse,
}
