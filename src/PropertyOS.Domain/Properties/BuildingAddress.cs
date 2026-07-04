using PropertyOS.Domain.Properties.Enums;

namespace PropertyOS.Domain.Properties;

/// <summary>
/// 1:1 address extension for a Building.
/// Maps to the PostgreSQL table: <c>building_addresses</c> (Module 4 §4.2).
///
/// Design decisions:
/// - Split from buildings to keep the buildings table lean and because address data
///   has a distinct query pattern (location-based search/filtering).
/// - <c>company_id</c> is denormalized here to enable tenant-scoped indexing and
///   RLS policy evaluation without joining through the buildings table.
/// - The composite FK (company_id, building_id) → buildings(company_id, id) physically
///   prevents this address from referencing a building owned by another company.
///
/// No soft-delete — lifecycle is CASCADE only from the parent Building.
/// Created_by/updated_by audit columns are omitted (address has no actor attribution).
/// </summary>
public class BuildingAddress
{
    // ---------------------------------------------------------------------------
    // Primary Key
    // ---------------------------------------------------------------------------
    public Guid Id { get; private set; }

    // ---------------------------------------------------------------------------
    // Composite hierarchy key — both columns denormalized for composite FK integrity.
    // ---------------------------------------------------------------------------

    /// <summary>UUID NOT NULL REFERENCES buildings(id) ON DELETE CASCADE. Unique (1:1).</summary>
    public Guid BuildingId { get; private set; }

    /// <summary>UUID NOT NULL — denormalized from buildings.company_id. See §4.0.</summary>
    public Guid CompanyId { get; private set; }

    // ---------------------------------------------------------------------------
    // Jordanian address hierarchy
    // ---------------------------------------------------------------------------

    /// <summary>governorate_enum NOT NULL — one of Jordan's 12 محافظات.</summary>
    public Governorate Governorate { get; private set; }

    /// <summary>لواء — district. VARCHAR(150) NOT NULL.</summary>
    public string District { get; private set; } = string.Empty;

    /// <summary>منطقة / neighborhood. VARCHAR(150) NULL — some rural districts lack named areas.</summary>
    public string? Area { get; private set; }

    /// <summary>VARCHAR(255) NULL.</summary>
    public string? StreetName { get; private set; }

    /// <summary>الترقيم الوطني — building plate number. VARCHAR(50) NULL.</summary>
    public string? BuildingPlateNumber { get; private set; }

    /// <summary>Critical in the Jordanian market — landmark-based navigation. VARCHAR(255) NULL.</summary>
    public string? NearestLandmark { get; private set; }

    /// <summary>Rarely used in Jordan. VARCHAR(20) NULL.</summary>
    public string? PostalCode { get; private set; }

    /// <summary>
    /// Convenience display cache for UI/printing. TEXT NULL.
    /// Composed and maintained by application code when any structured address field changes.
    /// NOT a source of truth; NOT trigger-maintained.
    /// </summary>
    public string? FullAddressText { get; private set; }

    // ---------------------------------------------------------------------------
    // Audit columns — created_at / updated_at only (no actor attribution, no soft-delete)
    // ---------------------------------------------------------------------------

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // ---------------------------------------------------------------------------
    // Navigation
    // ---------------------------------------------------------------------------

    /// <summary>Back reference to the owning building.</summary>
    public Building? Building { get; private set; }

    // ---------------------------------------------------------------------------
    // EF Core requires a parameterless constructor (private to prevent misuse).
    // ---------------------------------------------------------------------------
    private BuildingAddress() { }

    // ---------------------------------------------------------------------------
    // Factory method
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Creates a new BuildingAddress in the same transaction as its parent Building.
    /// <paramref name="buildingId"/> and <paramref name="companyId"/> must match the
    /// parent building (enforced at DB level by composite FK).
    /// </summary>
    public static BuildingAddress Create(
        Guid buildingId,
        Guid companyId,
        Governorate governorate,
        string district,
        DateTimeOffset createdAt,
        string? area = null,
        string? streetName = null,
        string? buildingPlateNumber = null,
        string? nearestLandmark = null,
        string? postalCode = null,
        string? fullAddressText = null)
    {
        if (buildingId == Guid.Empty)
            throw new ArgumentException("BuildingId must be a valid non-empty Guid.", nameof(buildingId));
        if (companyId == Guid.Empty)
            throw new ArgumentException("CompanyId must be a valid non-empty Guid.", nameof(companyId));
        if (string.IsNullOrWhiteSpace(district))
            throw new ArgumentException("District is required.", nameof(district));

        return new BuildingAddress
        {
            Id = Guid.Empty,
            BuildingId = buildingId,
            CompanyId = companyId,
            Governorate = governorate,
            District = district.Trim(),
            Area = string.IsNullOrWhiteSpace(area) ? null : area.Trim(),
            StreetName = string.IsNullOrWhiteSpace(streetName) ? null : streetName.Trim(),
            BuildingPlateNumber = string.IsNullOrWhiteSpace(buildingPlateNumber) ? null : buildingPlateNumber.Trim(),
            NearestLandmark = string.IsNullOrWhiteSpace(nearestLandmark) ? null : nearestLandmark.Trim(),
            PostalCode = string.IsNullOrWhiteSpace(postalCode) ? null : postalCode.Trim(),
            FullAddressText = string.IsNullOrWhiteSpace(fullAddressText) ? null : fullAddressText.Trim(),
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
        };
    }

    // ---------------------------------------------------------------------------
    // Mutation methods
    // ---------------------------------------------------------------------------

    /// <summary>Updates the structured address fields and refreshes the display cache.</summary>
    public void UpdateAddress(
        Governorate governorate,
        string district,
        string? area,
        string? streetName,
        string? buildingPlateNumber,
        string? nearestLandmark,
        string? postalCode,
        string? fullAddressText,
        DateTimeOffset updatedAt)
    {
        if (string.IsNullOrWhiteSpace(district))
            throw new ArgumentException("District is required.", nameof(district));

        Governorate = governorate;
        District = district.Trim();
        Area = string.IsNullOrWhiteSpace(area) ? null : area.Trim();
        StreetName = string.IsNullOrWhiteSpace(streetName) ? null : streetName.Trim();
        BuildingPlateNumber = string.IsNullOrWhiteSpace(buildingPlateNumber) ? null : buildingPlateNumber.Trim();
        NearestLandmark = string.IsNullOrWhiteSpace(nearestLandmark) ? null : nearestLandmark.Trim();
        PostalCode = string.IsNullOrWhiteSpace(postalCode) ? null : postalCode.Trim();
        FullAddressText = string.IsNullOrWhiteSpace(fullAddressText) ? null : fullAddressText.Trim();
        UpdatedAt = updatedAt;
    }
}
