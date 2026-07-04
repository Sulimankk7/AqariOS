using PropertyOS.Domain.Common;
using PropertyOS.Domain.Properties.Enums;

namespace PropertyOS.Domain.Properties;

/// <summary>
/// Represents a rentable apartment unit within a floor.
/// Maps to the PostgreSQL table: <c>apartments</c> (Module 4 §4.4).
///
/// Hierarchy: Company → Building → Floor → Apartment.
///
/// <b>Denormalized fields:</b>
///   <c>company_id</c> and <c>building_id</c> are denormalized from the parent
///   floor/building for RLS policy evaluation and high-traffic query hot paths
///   (see Module 4 §4.0). The database enforces consistency via composite FKs.
///
/// <b>Composite FK integrity:</b>
///   The database enforces (company_id, building_id, floor_id)
///   → floors(company_id, building_id, id) via a composite candidate key on floors.
///   This proves apartment.company_id = floor.company_id AND
///   apartment.building_id = floor.building_id atomically at INSERT/UPDATE time.
///
/// <b>occupancy_status — DEFERRED OWNERSHIP (Module 5):</b>
///   This column is a denormalized cache. The authoritative state is an active
///   <c>lease_contracts</c> row. The trigger that refreshes this column
///   (AFTER INSERT OR UPDATE OF status ON lease_contracts) is defined in Module 5.
///   In Phase 1 the column defaults to <c>vacant</c> and is correct by definition.
///   See OccupancyStatus enum for full documentation.
///
/// Soft-delete: YES (standard deleted_at / deleted_by).
/// RLS: ENABLE + FORCE; policy scopes by company_id via app.current_company_id.
/// Scale target: 2,000,000+ rows — no partitioning, composite indexes are the
/// scaling mechanism (see idx_apartments_company_occupancy, etc.).
/// </summary>
public class Apartment : ISoftDeletable
{
    // ---------------------------------------------------------------------------
    // Primary Key
    // ---------------------------------------------------------------------------
    public Guid Id { get; private set; }

    // ---------------------------------------------------------------------------
    // Composite hierarchy keys — all three denormalized for FK integrity and RLS.
    // ---------------------------------------------------------------------------

    /// <summary>UUID NOT NULL — denormalized from floors/buildings. See §4.0.</summary>
    public Guid CompanyId { get; private set; }

    /// <summary>UUID NOT NULL — denormalized from floors.building_id. See §4.0.</summary>
    public Guid BuildingId { get; private set; }

    /// <summary>UUID NOT NULL REFERENCES floors(id) ON DELETE RESTRICT.</summary>
    public Guid FloorId { get; private set; }

    // ---------------------------------------------------------------------------
    // Unit identity
    // ---------------------------------------------------------------------------

    /// <summary>
    /// As it appears on the unit door e.g. "301", "G-2". VARCHAR(20) NOT NULL.
    /// Unique per building (uq_apartments_building_unit_number WHERE deleted_at IS NULL).
    /// A unit number may repeat across different buildings of the same company,
    /// but never within one building regardless of floor.
    /// </summary>
    public string UnitNumber { get; private set; } = string.Empty;

    // ---------------------------------------------------------------------------
    // Ownership model
    // ---------------------------------------------------------------------------

    /// <summary>ownership_status_enum NOT NULL DEFAULT 'company_owned'.</summary>
    public OwnershipStatus OwnershipStatus { get; private set; } = OwnershipStatus.CompanyOwned;

    /// <summary>
    /// VARCHAR(255) NULL. Required (DB CHECK) when OwnershipStatus = ThirdPartyOwned.
    /// chk_apartments_external_owner_required enforces this at the DB level.
    /// </summary>
    public string? ExternalOwnerName { get; private set; }

    /// <summary>VARCHAR(20) NULL.</summary>
    public string? ExternalOwnerPhone { get; private set; }

    // ---------------------------------------------------------------------------
    // Occupancy status — trigger-maintained cache (Module 5 owns the write trigger)
    // ---------------------------------------------------------------------------

    /// <summary>
    /// occupancy_status_enum NOT NULL DEFAULT 'vacant'.
    /// DEFERRED: Module 5 will register the AFTER INSERT OR UPDATE OF status ON
    /// lease_contracts trigger that keeps this column consistent. Until then,
    /// only default and manual updates via application code apply.
    /// </summary>
    public OccupancyStatus OccupancyStatus { get; private set; } = OccupancyStatus.Vacant;

    // ---------------------------------------------------------------------------
    // Physical attributes
    // ---------------------------------------------------------------------------

    /// <summary>NUMERIC(7,2) NOT NULL. DB CHECK: area_sqm > 0.</summary>
    public decimal AreaSqm { get; private set; }

    /// <summary>SMALLINT NOT NULL DEFAULT 0. DB CHECK: bedrooms >= 0.</summary>
    public short Bedrooms { get; private set; } = 0;

    /// <summary>SMALLINT NOT NULL DEFAULT 0. DB CHECK: bathrooms >= 0.</summary>
    public short Bathrooms { get; private set; } = 0;

    // ---------------------------------------------------------------------------
    // Pricing
    // ---------------------------------------------------------------------------

    /// <summary>NUMERIC(12,3) NULL. DB CHECK: base_rent_amount IS NULL OR base_rent_amount > 0.</summary>
    public decimal? BaseRentAmount { get; private set; }

    /// <summary>CHAR(3) NOT NULL DEFAULT 'JOD'. ISO 4217.</summary>
    public string BaseRentCurrency { get; private set; } = "JOD";

    // ---------------------------------------------------------------------------
    // Active flag
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Unit taken permanently out of service (merged, converted to non-residential).
    /// BOOLEAN NOT NULL DEFAULT true. Distinct from OccupancyStatus.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    // ---------------------------------------------------------------------------
    // Audit columns
    // ---------------------------------------------------------------------------

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>UUID NULL REFERENCES users(id) ON DELETE SET NULL.</summary>
    public Guid? CreatedBy { get; private set; }

    /// <summary>UUID NULL REFERENCES users(id) ON DELETE SET NULL.</summary>
    public Guid? UpdatedBy { get; private set; }

    // ---------------------------------------------------------------------------
    // Soft-delete columns (ISoftDeletable)
    // ---------------------------------------------------------------------------

    public DateTimeOffset? DeletedAt { get; private set; }

    /// <summary>UUID NULL REFERENCES users(id) ON DELETE SET NULL.</summary>
    public Guid? DeletedBy { get; private set; }

    // ---------------------------------------------------------------------------
    // EF Core requires a parameterless constructor (private to prevent misuse).
    // ---------------------------------------------------------------------------
    private Apartment() { }

    // ---------------------------------------------------------------------------
    // Factory method
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Creates a new Apartment entity.
    /// All three hierarchy keys (company_id, building_id, floor_id) are required
    /// and validated cheaply here; the composite FK in the database is authoritative
    /// for cross-entity consistency.
    /// <paramref name="externalOwnerName"/> is required when
    /// <paramref name="ownershipStatus"/> is ThirdPartyOwned (DB CHECK authoritative).
    /// </summary>
    public static Apartment Create(
        Guid companyId,
        Guid buildingId,
        Guid floorId,
        string unitNumber,
        decimal areaSqm,
        DateTimeOffset createdAt,
        Guid? createdBy,
        OwnershipStatus ownershipStatus = OwnershipStatus.CompanyOwned,
        string? externalOwnerName = null,
        string? externalOwnerPhone = null,
        short bedrooms = 0,
        short bathrooms = 0,
        decimal? baseRentAmount = null,
        string baseRentCurrency = "JOD")
    {
        if (companyId == Guid.Empty)
            throw new ArgumentException("CompanyId must be a valid non-empty Guid.", nameof(companyId));
        if (buildingId == Guid.Empty)
            throw new ArgumentException("BuildingId must be a valid non-empty Guid.", nameof(buildingId));
        if (floorId == Guid.Empty)
            throw new ArgumentException("FloorId must be a valid non-empty Guid.", nameof(floorId));
        if (string.IsNullOrWhiteSpace(unitNumber))
            throw new ArgumentException("Unit number is required.", nameof(unitNumber));
        if (areaSqm <= 0)
            throw new ArgumentOutOfRangeException(nameof(areaSqm), "Area must be positive.");
        if (bedrooms < 0)
            throw new ArgumentOutOfRangeException(nameof(bedrooms), "Bedrooms cannot be negative.");
        if (bathrooms < 0)
            throw new ArgumentOutOfRangeException(nameof(bathrooms), "Bathrooms cannot be negative.");
        if (baseRentAmount.HasValue && baseRentAmount.Value <= 0)
            throw new ArgumentOutOfRangeException(nameof(baseRentAmount), "Base rent must be positive when provided.");

        // Mirror DB chk_apartments_external_owner_required (DB is authoritative).
        if (ownershipStatus == OwnershipStatus.ThirdPartyOwned && string.IsNullOrWhiteSpace(externalOwnerName))
            throw new ArgumentException(
                "ExternalOwnerName is required when ownership_status is third_party_owned.",
                nameof(externalOwnerName));

        return new Apartment
        {
            Id = Guid.Empty,
            CompanyId = companyId,
            BuildingId = buildingId,
            FloorId = floorId,
            UnitNumber = unitNumber.Trim(),
            OwnershipStatus = ownershipStatus,
            ExternalOwnerName = string.IsNullOrWhiteSpace(externalOwnerName) ? null : externalOwnerName.Trim(),
            ExternalOwnerPhone = string.IsNullOrWhiteSpace(externalOwnerPhone) ? null : externalOwnerPhone.Trim(),
            OccupancyStatus = OccupancyStatus.Vacant,
            AreaSqm = areaSqm,
            Bedrooms = bedrooms,
            Bathrooms = bathrooms,
            BaseRentAmount = baseRentAmount,
            BaseRentCurrency = string.IsNullOrWhiteSpace(baseRentCurrency) ? "JOD" : baseRentCurrency.ToUpperInvariant(),
            IsActive = true,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            CreatedBy = createdBy,
            UpdatedBy = createdBy,
        };
    }

    // ---------------------------------------------------------------------------
    // Mutation methods
    // ---------------------------------------------------------------------------

    /// <summary>Standard soft-delete. All lease/payment/maintenance history remains intact.</summary>
    public void SoftDelete(DateTimeOffset deletedAt, Guid? deletedBy)
    {
        if (DeletedAt.HasValue) return;
        DeletedAt = deletedAt;
        DeletedBy = deletedBy;
        IsActive = false;
        UpdatedAt = deletedAt;
        UpdatedBy = deletedBy;
    }

    /// <summary>Deactivates the unit (permanently out of service, not just unoccupied).</summary>
    public void Deactivate(DateTimeOffset updatedAt, Guid? updatedBy)
    {
        IsActive = false;
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    /// <summary>
    /// Updates asking rent. Financially significant — will be audit-logged by caller.
    /// DB CHECK: base_rent_amount IS NULL OR base_rent_amount > 0 is authoritative.
    /// </summary>
    public void UpdateBaseRent(decimal? amount, string currency, DateTimeOffset updatedAt, Guid? updatedBy)
    {
        if (amount.HasValue && amount.Value <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Rent amount must be positive.");
        BaseRentAmount = amount;
        BaseRentCurrency = string.IsNullOrWhiteSpace(currency) ? "JOD" : currency.ToUpperInvariant();
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }
}
