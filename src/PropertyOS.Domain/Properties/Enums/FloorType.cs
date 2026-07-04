namespace PropertyOS.Domain.Properties.Enums;

/// <summary>
/// Floor classification. Maps to PostgreSQL enum <c>floor_type_enum</c>.
/// Approved values per Module 4 §4.3.
/// </summary>
public enum FloorType
{
    Basement,
    Ground,
    Regular,
    Roof,
}
