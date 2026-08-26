namespace PropertyOS.Domain.UtilityBills.Enums;

/// <summary>
/// The type of utility service associated with a tenant's linked account.
/// Maps to the PostgreSQL enum type: utility_type_enum.
/// </summary>
public enum UtilityType
{
    Electricity,
    Water
}
