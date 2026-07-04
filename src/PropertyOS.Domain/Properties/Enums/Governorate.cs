namespace PropertyOS.Domain.Properties.Enums;

/// <summary>
/// Jordan's 12 governorates (محافظات). Maps to PostgreSQL enum <c>governorate_enum</c>.
/// Modeled as an enum — not a reference table — per Module 4 §4.0 design decision:
/// the domain is small (exactly 12), stable (changes only on administrative reform
/// timescales), and benefits from referential-integrity-style guarantees without
/// lookup-table overhead.
///
/// Approved values per Module 4 §4.2. Do NOT add or rename values without a migration.
/// </summary>
public enum Governorate
{
    Amman,
    Zarqa,
    Irbid,
    Balqa,
    Madaba,
    Karak,
    Tafilah,
    Maan,
    Aqaba,
    Ajloun,
    Jerash,
    Mafraq,
}
