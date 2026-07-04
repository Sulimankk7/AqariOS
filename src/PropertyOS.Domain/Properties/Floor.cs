using PropertyOS.Domain.Common;
using PropertyOS.Domain.Properties.Enums;

namespace PropertyOS.Domain.Properties;

/// <summary>
/// Represents a single floor within a building.
/// Maps to the PostgreSQL table: <c>floors</c> (Module 4 §4.3).
///
/// Hierarchy: Company → Building → Floor → Apartment.
///
/// <b>Denormalized fields:</b>
///   <c>apartments_count</c> is a trigger-maintained cache updated by the same
///   AFTER INSERT OR UPDATE OF deleted_at ON apartments trigger that updates
///   buildings.total_apartments_count. Application code must NOT maintain this
///   counter; the trigger is the sole owner.
///
/// <b>Composite FK integrity:</b>
///   The database enforces (company_id, building_id) → buildings(company_id, id)
///   via a candidate key on buildings. This physically prevents a floor from
///   referencing a building from a different company.
///
/// Soft-delete: YES (standard deleted_at / deleted_by).
/// RLS: ENABLE + FORCE; policy scopes by company_id via app.current_company_id.
/// </summary>
public class Floor : ISoftDeletable
{
    // ---------------------------------------------------------------------------
    // Primary Key
    // ---------------------------------------------------------------------------
    public Guid Id { get; private set; }

    // ---------------------------------------------------------------------------
    // Composite hierarchy keys — both denormalized for FK integrity and RLS.
    // ---------------------------------------------------------------------------

    /// <summary>UUID NOT NULL — denormalized from buildings.company_id. See §4.0.</summary>
    public Guid CompanyId { get; private set; }

    /// <summary>UUID NOT NULL REFERENCES buildings(id) ON DELETE RESTRICT.</summary>
    public Guid BuildingId { get; private set; }

    // ---------------------------------------------------------------------------
    // Floor identity
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Signed sort-order key: negative for basements (-1, -2...), 0 for ground,
    /// positive for upper floors, roof gets the next integer above the highest
    /// regular floor. SMALLINT NOT NULL. This is a sort key, NOT a display label.
    /// Unique per building (uq_floors_building_floor_number WHERE deleted_at IS NULL).
    /// </summary>
    public short FloorNumber { get; private set; }

    /// <summary>Human-facing display label e.g. "أرضي", "الطابق الأول", "روف". VARCHAR(50) NOT NULL.</summary>
    public string FloorLabel { get; private set; } = string.Empty;

    /// <summary>floor_type_enum NOT NULL DEFAULT 'regular'.</summary>
    public FloorType FloorType { get; private set; } = FloorType.Regular;

    /// <summary>
    /// Denormalized apartment counter. INTEGER NOT NULL DEFAULT 0.
    /// Maintained exclusively by the PostgreSQL trigger. Application code must NOT
    /// write this property directly.
    /// </summary>
    public int ApartmentsCount { get; private set; } = 0;

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
    // Navigation
    // ---------------------------------------------------------------------------

    /// <summary>Apartments on this floor.</summary>
    public IReadOnlyCollection<Apartment> Apartments => _apartments.AsReadOnly();
    private readonly List<Apartment> _apartments = new();

    // ---------------------------------------------------------------------------
    // EF Core requires a parameterless constructor (private to prevent misuse).
    // ---------------------------------------------------------------------------
    private Floor() { }

    // ---------------------------------------------------------------------------
    // Factory method
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Creates a new Floor entity.
    /// The composite FK (company_id, building_id) → buildings(company_id, id) is
    /// physically enforced by the database; this factory requires both values to be
    /// present and non-empty as a cheap client-side guard.
    /// </summary>
    public static Floor Create(
        Guid companyId,
        Guid buildingId,
        short floorNumber,
        string floorLabel,
        DateTimeOffset createdAt,
        Guid? createdBy,
        FloorType floorType = FloorType.Regular)
    {
        if (companyId == Guid.Empty)
            throw new ArgumentException("CompanyId must be a valid non-empty Guid.", nameof(companyId));
        if (buildingId == Guid.Empty)
            throw new ArgumentException("BuildingId must be a valid non-empty Guid.", nameof(buildingId));
        if (string.IsNullOrWhiteSpace(floorLabel))
            throw new ArgumentException("Floor label is required.", nameof(floorLabel));

        return new Floor
        {
            Id = Guid.Empty,
            CompanyId = companyId,
            BuildingId = buildingId,
            FloorNumber = floorNumber,
            FloorLabel = floorLabel.Trim(),
            FloorType = floorType,
            ApartmentsCount = 0,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            CreatedBy = createdBy,
            UpdatedBy = createdBy,
        };
    }

    // ---------------------------------------------------------------------------
    // Mutation methods
    // ---------------------------------------------------------------------------

    /// <summary>Standard soft-delete. All child apartments and their history remain intact.</summary>
    public void SoftDelete(DateTimeOffset deletedAt, Guid? deletedBy)
    {
        if (DeletedAt.HasValue) return;
        DeletedAt = deletedAt;
        DeletedBy = deletedBy;
        UpdatedAt = deletedAt;
        UpdatedBy = deletedBy;
    }

    /// <summary>Updates floor display label and type (number is structural — not mutated after creation).</summary>
    public void UpdateLabel(string floorLabel, FloorType floorType, DateTimeOffset updatedAt, Guid? updatedBy)
    {
        if (string.IsNullOrWhiteSpace(floorLabel))
            throw new ArgumentException("Floor label is required.", nameof(floorLabel));
        FloorLabel = floorLabel.Trim();
        FloorType = floorType;
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }
}
